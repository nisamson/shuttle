CREATE ROLE [ShuttleQuartzUser];
GRANT SELECT, INSERT, UPDATE, DELETE ON SCHEMA::[qrtz] TO [ShuttleQuartzUser];

CREATE ROLE [ShuttleApiUser];
GRANT SELECT, INSERT, UPDATE, DELETE ON SCHEMA::[dbo] TO [ShuttleApiUser];

CREATE USER [shl-app-umi] FROM EXTERNAL PROVIDER;

ALTER ROLE [ShuttleQuartzUser] ADD MEMBER [shl-app-umi];
ALTER ROLE [ShuttleApiUser] ADD MEMBER [shl-app-umi];