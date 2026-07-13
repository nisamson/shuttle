
# Tales From The Datacube: You May Not Like It, But This Is What A Medoid Skater Looks Like

## What is a datacube?

I built a [datacube!](https://youtu.be/lwEwxKkCGJE?si=xHkHgtAG436m5lns&t=613)

More precisely, I built my own representation of the SHL player database so I could run data analysis on it. I'm hoping to expand it
in the future to include more data about player hockey statistics, team data, etc. but for now I've got the current player 
information with the simulation attributes; you know, those things you buy with your hard-earned TPE!

[Data munging](https://en.wikipedia.org/wiki/Data_wrangling) is an important step when trying to analyze data and the raw player data is... kind of messy. Very messy.
The league data has evolved throughout the years and a lot of that evolution has been reflected in the player data; several 
hundred lines of code in the Shuttle project are dedicated to converting the weirder parts of the data into a consistent format.
The way the league data is structured itself could be the subject of a lengthy blog post, but that's not what this post is about.

## What this post is about

This post is not about what the greatest skaters look like or what the worst skaters look like, or what goalies look like at all.
This post is about what a typical skater's "shape" is.

### What is a player "shape"?

Every player has a set of attributes that you can see in the portal. If you turn these attributes into a vector, you've got a point in a high-dimensional space
Skater attributes can be viewed as a point or vector in a 23-dimensional space (13-dimensional for goalies), ignoring the attributes that are the same for every player.

What I'm calling the "shape" of a player is essentially this: 

![TheShapeOfAPlayer.png](https://i.imgur.com/NofppHm.png).

Ignore the numbers. The "shape" is the relative lengths of the bars. Why is this important? Because the relative lengths of the bars tells you about what a player's job is.

Anyone familiar with hockey knows that a player with a low faceoff stat probably isn't a center. We'd expect a center to their faceoff stat be a larger bar compared to others than for non-centers.
A player with moderate shooting range and high shooting accuracy is probably a sniper. A player with high checking and low shooting is probably more on the enforcer side than the offensive side of the game.

### What is a medoid?

A "medoid" is the point in a set of points that's the closest to the center (centroid) of the set. In a way, it's the player with the most "typical" shape.
This shape is different for rookies, forwards, defensemen, and centers have their own shape since they're the only ones that invest in the faceoff stat heavily. 

### Where are the goalies?

Goalies are weird and scary, but more to the point, they have a different set of attributes that skaters and don't neatly separate into categories
the same way skaters do. 

## The Typical-est Skaters in the History of the Simulation Hockey League

### Skaters, as a whole

Congratulations, [Friedensreich Hundertwasser](https://portal.simulationhockey.com/player/38)! You were the most typical
skater in the history of the SHL (in available data going back to 2023)!

![typical_skater.png](https://imgur.com/WBAXlPl.png)

No particular surprises here, aggression and fighting were and remain dump stats. Fighting is rare and largely pointless in the SHL,
and aggression is basically a "free penalties" stat. Other than those outliers,
the typical skater is a well-rounder player, slightly offensively oriented,
with high physical statistics. The offensive lean is likely related to the fact that a team
has more forwards than defensemen, so this is skewed in that direction.

### Forwards, as a whole

Congratulations, [Boogan McGillicuddy](https://portal.simulationhockey.com/player/123)! You were the most typical forward in the history of the SHL!

![typical_forward.png](https://imgur.com/PEtlnr3.png)

Compared to the most typical skater, the most typical forward has a higher faceoff stat, and lower defensive stats. Not
very surprising.

#### Centers

Congratulations, [Bob Bergen Jr.](https://portal.simulationhockey.com/player/2004)! You are the most typical center in the history of the SHL (despite your current inactivity as of the time of writing)!

![typical_center.png](https://imgur.com/txYKbTl.png)

As a center, he has a higher faceoff stat than the most typical forward, slightly lower defensive stats, and slightly higher offensive stats.
There really aren't a lot of surprises here, the goal here *was* to find the most **typical** players.

#### Wingers

Congratulations, [Salsa Steve](https://portal.simulationhockey.com/player/2542)! You are the most typical winger in the history of the SHL!
Keep up the good work, you're earlier in your career and have the potential to remain the most typical winger for seasons to come!

![typical_winger.png](https://imgur.com/XeGUkiJ.png)

Wingers! High-octane offense, low-responsibity defense. Shocker. I suspect some wingers wanted some flexibility with that faceoff stat.

### Defensemen

Congratulations, [Amélie Delacroix](https://portal.simulationhockey.com/player/2543)! You are the most typical defenseman in the history of the SHL!

![typical_defenseman.png](https://imgur.com/KcueXbr.png)

Wow, the second one from S88 in this post! Low-octane defensive responsibility, but with two-way upside.

## Conclusion, Part 1

I would guess that as players get right on the edge of SHL competition level, they get right on that edge of being the most typical player in their position.
This might be because of players that cap out right at the end of SMJHL eligibility looking right about the same as the others since you're limited in your ability to specialize.
The nature of the TPE cap in the SMJHL creates two seasons of players who are trying to be the most effective they can in their position.

Probably worth another look in the future at purely players above the cap.

Finally, here's a chart with all of the medoids together, so you can see the differences between the positions.

![typical_players.png](https://imgur.com/mnzzu7j.png)

## What does the *computer* think the 5 categories are/should be?

We arbitrarily select players into 5 categories: centers, wingers, defensemen, forwards, and skaters. But what does the computer think?
I ran a [k-means clustering](https://en.wikipedia.org/wiki/K-means_clustering) algorithm on the skater data and asked it to find 5 clusters. The results are below.

K-means clustering is a machine learning algorithm where the computer attempts to find "k" categories of data given a set of that data.
In this case, I fed the same skaters from above into the algorithm, told it to find 5 categories, and took that data and graphed it.

![skater_clusters.png](https://imgur.com/ySBrykE.png)

Wow! The computer actually came up with similar categories to ones used in real hockey.

### A: Wingers

Again, high-octane offense, low-responsibility defense. The computer distinguished them from centers and it's even more highly accentuated here.
That's in part because the computer doesn't care about preexisting categories, it just looks at the data and finds patterns.

### B: Centers

Centers like faceoffs! Still offensively oriented, but with more of a two-way game. Yet again, the computer identified a real hockey position.

### C: Offensive Defensemen

The defensemen are interesting to me, as the computer identified two different types which match to real-world positional classifications.

Category C is your Quinn Hugheses and Cale Makars of the world, offensively oriented defensemen but with more variation in their defensive stats.

### D: Defensive Defensemen

Jacob Slavin and Gustav Forsling are your modern-day defensive defensemen, the shutdown guys who keep the puck where it should be: over there and not over here.
Lower offensive prowess but much more consistent defensive stats. 

### E: Goons and Grinders

This one surprised me (and the small panel I asked to help me interpret the data). Category E is your grinders and enforcers, the players who punch and don't do much else.
They rack up penalty minutes, take the occasional faceoffs, and beat the crap out of anyone who looks the wrong way at their skilled teammates.

There isn't a lot of reward for being a category E player in the SHL, but they apparently do exist. I've run into a few users who like making these kinds of players,
and apparently some of them survive long enough to make it past the cutoff threshold.

## Conclusion, Part 2

The computer did a good job of identifying common categories of player in the SHL. I was impressed at how well it did; I wasn't sure what to expect beforehand given the limited
data available, but k-means clustering is known to provide some good results with limited data.

## Methodology Notes

### TPE Cutoff

To ensure the data was representative of players with some flexibility to specialize, any players with less than 250 TPE were filtered out of this analysis.

### Normalization

To avoid the total TPE of a player affecting comparisons of shape, the attributes are [L1-normalized](https://blog.mlreview.com/l1-norm-regularization-and-sparsity-explained-for-dummies-5b0e4be3938a) so that a player with 400 TPE and a player with 2000 TPE, assuming they're playing the same role,
will have about the same shape. This is particularly important because there are so few players that have existed across the history of the SHL, only 2500 or so. If you're not familiar with statistics, then 1) thank you for reading this far and 2) 2500 data points is *tiny* when you're doing data analysis.
It's even worse when you have to discard about half of those data points because they don't have enough TPE to be "typical" players.