from random import randint

T = 50
currT = T

# The function for rolling to spawn. Uses currT above (global)
def rollSpawn():
    global currT
    
    if currT == 0:
        currT = T
        return True
    else:
        hit = (randint(0,255) == 0)
        if hit:
            currT = T
        else:
            currT = currT - 1
            
        return hit
    
# Performs one run of our game, returning a boolean for success.
# Proceeds in intervals of 1 frame, even during player motion.
def trial(t_target,t_move):
    nKilled = 0
    remTime = t_target + t_move
    spawned = False
    remTravel = t_move

    # Clear timeout!
    global currT
    currT = T
    
    # The main 'event' loop
    while nKilled < 3 and remTime > 0:
        # If we're not spawned, try to spawn us
        if not spawned:
            spawned = rollSpawn()

        # If we're travelling, reduce the 'distance' left
        # Otherwise, if an enemy is spawned, kill it
        if nKilled == 1 and remTravel > 0:
            remTravel -= 1
        else:
            if spawned:
                nKilled += 1
                spawned = False

        remTime -= 1

        
    # Return whether we succeeded
    return nKilled == 3

# Performs an endless amount of the above trials to approximate the probability of success.
# Will converge very slowly - see central limit theorem for details!
def approxProb(t_target, t_move):
    nTrials = 0
    nSuccesses = 0
    while True:
        if trial(t_target,t_move):
            nSuccesses += 1
        if nSuccesses > 0 and nTrials % 10000 == 0:
            odds = nTrials / nSuccesses
            print("1 in " + str(odds) + ", " + str(nTrials))
            
        nTrials += 1
        
# Gave me around 1 in 1060, close to the result
approxProb(35,19)
