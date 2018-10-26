# I~See~U-Game

#### From Game Science: Group 14
### Notes: before running game in Unity, must put ml-agents folder inside Assess. 

## AI Defintion:
##### Game rules: 
-Player can see Robot only in 2 units distance; 
-Robot can see Robot only in 3 units distance; 
-Player can move 2 unit each turn; 
-Robot can move 1 unit each turn;
	-Three goals exist on the map
	-Robot needs to choose and try to reach two of them

##### State is a 12*12 matrix, recording player, robot and environment’s position for each Turn.

##### Action is robot and player’s movement: up down left and right

##### Reward function: R(S, a) = [d(player, goal) / v(player) + d(player, robot) / v(player-robot) - d(robot, goal) / v(robot)] * coefficient

##### Policy: a 144* 4 matrix, storing the four directions’ probability for each slot

##### How to update policy (learning)?
Learning after each game
Suppose we record {S1, …, Sn} states for last game.
For each S1 to S2, Minimax algorithm will take player's movement as first Min value, and then calculate utility of the following actions (up to level 3 in Minimax search tree) based on defined reward function and previous policy. 
By predicting the utilities of next movement, we modify probability distribution of current slot.

##### We would have four different policies to satisfy our game rules
One at beginning, which would lead the robot to either one of three goals.
When it reaches the first goal, say Goal A, its policy would be changed to a different one, since it cannot revisit the Goal A. 
Hence, we would have 1+3 = 4 policies. For each training after the game, we divide the game into two parts and store the result policy in different arrays.

##### Real-time policy change during the game:
In the case that robot “sees” Player during the game, it cannot go towards the Player, since the robot would be exposed under the sight of Player and be caught certainly. Hence, we would temporarily change the policy by doing Minimax to make sure the robot goes to other slots. 


