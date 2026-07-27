# CI/CD release prerequisites (issue #85)

The **Release** workflow (`.github/workflows/release.yml`) deploys the production stack in a
fixed order — **migrations → backend (Aspire) → SPA (SWA)** — from a GitHub Actions runner.
It authenticates to Azure with **OIDC federated credentials** (no stored client secret) and is
**manual-only** (`workflow_dispatch`), gated to the `main` branch and the `production`
environment.

This document records the one-time, out-of-band setup so it can be reproduced or audited.
The mechanics of each deploy step live in the `deploy-to-production` skill and the
`scripts/Deploy-*.ps1` scripts.

## Identity

| Piece | Value |
| ----- | ----- |
| App registration | `<APP_REGISTRATION_NAME>` |
| Application (client) ID | `<APP_CLIENT_ID>` |
| Tenant ID | `<TENANT_ID>` |
| Subscription | `<SUBSCRIPTION_NAME>` (`<SUBSCRIPTION_ID>`) |

### 1. App registration + service principal

```powershell
$appId = az ad app create --display-name "<APP_REGISTRATION_NAME>" `
    --sign-in-audience AzureADMyOrg --query appId -o tsv
az ad sp create --id $appId
```

### 2. Federated credential (GitHub OIDC — no client secret)

A single credential scoped to the `production` **environment** (least privilege). The workflow's
`deploy` job declares `environment: production`, which is reviewer-gated and branch-locked to
`main` — so this subject can only be assumed by an approved production run, not by arbitrary
workflows on `main`:

```powershell
$objId = az ad app show --id $appId --query id -o tsv

'{"name":"gh-env-production","issuer":"https://token.actions.githubusercontent.com","subject":"repo:nisamson/shuttle:environment:production","audiences":["api://AzureADTokenExchange"]}' `
  | Out-File -Encoding ascii fc.json
az ad app federated-credential create --id $objId --parameters '@fc.json'
Remove-Item fc.json
```

> Do **not** add a branch-scoped subject (e.g. `...:ref:refs/heads/main`). It would let any
> workflow running on `main` assume this privileged identity, bypassing the environment gate.

### 3. Azure role assignments

| Role | Scope | Why |
| ---- | ----- | --- |
| `Contributor` | RG `shlanalyticswest` | Aspire App Service deploy + provisioning |
| `AcrPush` | ACR `<ACR_NAME>` | `aspire deploy` pushes the API container image (Contributor alone can't push) |

```powershell
$sub   = "<SUBSCRIPTION_ID>"
$scope = "/subscriptions/$sub/resourceGroups/shlanalyticswest"
az role assignment create --assignee $appId --role "Contributor" --scope $scope

$acr = az acr show -n <ACR_NAME> -g shlanalyticswest --query id -o tsv
az role assignment create --assignee $appId --role "AcrPush" --scope $acr
```

### 4. Prod database user (for `dotnet ef database update`)

Migrations run as this SP against the prod `Shuttle` DB, so it must be a contained Entra DB
user. Run as the SQL server's **Entra admin**, connected to the `Shuttle` database (not
`master`):

```sql
IF NOT EXISTS (SELECT 1 FROM sys.database_principals WHERE name = '<APP_REGISTRATION_NAME>')
    CREATE USER [<APP_REGISTRATION_NAME>] FROM EXTERNAL PROVIDER;
ALTER ROLE db_ddladmin   ADD MEMBER [<APP_REGISTRATION_NAME>];  -- CREATE/ALTER/DROP for migrations
ALTER ROLE db_datareader ADD MEMBER [<APP_REGISTRATION_NAME>];
ALTER ROLE db_datawriter ADD MEMBER [<APP_REGISTRATION_NAME>];
```

> `db_ddladmin` covers standard migration DDL. If a future migration needs ownership-level
> operations (e.g. certain temporal-table changes), promote the user to `db_owner`.

## GitHub configuration

### Secrets (repo-level)

Set from the identity values above:

```powershell
gh secret set AZURE_CLIENT_ID       --repo nisamson/shuttle --body "<APP_CLIENT_ID>"
gh secret set AZURE_TENANT_ID       --repo nisamson/shuttle --body "<TENANT_ID>"
gh secret set AZURE_SUBSCRIPTION_ID --repo nisamson/shuttle --body "<SUBSCRIPTION_ID>"
# Production Application Insights resource name (Aspire AppHost `appInsightsName` parameter).
gh secret set APP_INSIGHTS_NAME     --repo nisamson/shuttle --body "<APP_INSIGHTS_NAME>"
```

### `production` environment

Restricted to the `main` branch **and** protected by a required reviewer, so every production
run (which includes prod DB migrations) pauses for manual approval. Substitute the reviewer's
numeric user id (`gh api users/<login> --jq .id`):

```powershell
# Branch policy: main only
gh api --method PUT  repos/nisamson/shuttle/environments/production `
  --input (echo '{"deployment_branch_policy":{"protected_branches":false,"custom_branch_policies":true}}')
gh api --method POST repos/nisamson/shuttle/environments/production/deployment-branch-policies -f name=main

# Required reviewer(s) — approval gate before any deploy proceeds
gh api --method PUT repos/nisamson/shuttle/environments/production `
  --input (echo '{"reviewers":[{"type":"User","id":<REVIEWER_USER_ID>}],"deployment_branch_policy":{"protected_branches":false,"custom_branch_policies":true}}')
```

## Running the workflow

Actions → **Release (Deploy to Production)** → *Run workflow* (from `main`), type `deploy`
in the confirm box. The single job runs migrations → backend → SPA in order and stops on the
first failure.

### Dry run (validate without deploying)

Check the **`dry_run`** box instead of typing `deploy`. The job still requires environment
approval and signs in via OIDC (so it exercises the real auth + DB connectivity path), but it
**changes nothing**:

- migrations: lists applied/pending migrations and writes the **idempotent SQL** (`migration.sql`)
  that a real apply would run — but does not apply it;
- backend: runs `aspire publish` (generates the deployment manifest) instead of `aspire deploy`;
- SPA: runs `dotnet publish` only, skipping the deployment-token lookup and the SWA deploy.

The generated `migration.sql` and Aspire manifest are uploaded as the **`release-dry-run`**
artifact for review before a real deployment.

## Known caveats

- **Role assignments are created once via a `FIRST_RUN` bootstrap, then stripped in CI.** The
  generated Azure infrastructure includes three RBAC role assignments (ACR `AcrPull` for the
  pull identity, the Aspire dashboard identity's RG-scoped `Contributor`, and the web app's
  `Website Contributor`). Creating them needs `Microsoft.Authorization/roleAssignments/write`,
  which **`Contributor` does not grant** and which the CI deploy principal intentionally lacks.

  `Shuttle.Backend.Aspire/AppHost.cs` reads a `FIRST_RUN` config flag:
  - **Bootstrap (one time):** run `aspire deploy` **locally** from an identity that can assign
    roles (Owner or User Access Administrator on the RG) with `FIRST_RUN=true`. This emits the
    role assignments and creates them:

    ```pwsh
    $env:FIRST_RUN = "true"
    ./scripts/Deploy-BackendAspire.ps1   # or: aspire deploy from Shuttle.Backend.Aspire
    Remove-Item Env:FIRST_RUN
    ```
  - **Steady state (every later deploy, incl. CI):** `FIRST_RUN` is unset, so the role
    assignments are stripped from the template and the deploy needs no `roleAssignments/write`.
    Azure incremental deployments never delete resources absent from the template, so the
    role assignments created during bootstrap remain in place and the app keeps working.

  Avoid the tempting shortcut of granting the deploy service principal
  `User Access Administrator` (or `Owner`): combined with its existing `Contributor`, that would
  let a compromised CI run grant itself any role and establish persistence. The `FIRST_RUN`
  bootstrap keeps the CI principal least-privileged.

- **`db_ddladmin` scope.** Covers standard migration DDL; promote the DB user to `db_owner` if a
  future migration needs ownership-level operations (e.g. certain temporal-table changes).
