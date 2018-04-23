# As described in pdf, derived from boss' run
t_move = 19

# Set this - here for the 2:06 pace.
t_wait = 35
t_target = t_wait + t_move

numTargets = 3

# The probability of 'numTargets' kills in t frames with:
#   - probability p of spawning on a 'standard' frame
#   - a spawn is forced after T (timeout) frames
#   - instantaneous kills
def pKillAllIn(t,p,T):
    # Clear the cache, used internally for the dynamic programming
    global giCache, maxT
    giCache = dict()
    maxT = T
    
    # Call main function body with appropriate parameters
    return _pKillAllIn(0,t,T,p)

# The probability of reaching 'numTargets' kills WITHIN the remaining time, 'remTime',
#   given that we have killed 'currKills'
#   the timeout is 'currT' (this starts at maxT, if a spawn fails it decreases by 1)
#   and pSpawn is the probability of spawning on a 'standard' frame
# Notice the parameters mimic the pdf, with pSpawn added.
def _pKillAllIn(currKills,remTime,currT,pSpawn):
    assert currT >= 0
    global giCache, maxT

    # If a base case, return answer. Order is crucial:
    # If we've run out of time, probability is 0
    if remTime < 0:
        return 0
    # If we've succeeded within the time, probability is 1
    if currKills == numTargets:
        return 1

    # If cached, return answer. Notice pSpawn fixed, but it's clean to include it in the key.
    key = (currKills,remTime,currT,pSpawn)
    if key in giCache:
        return giCache[key]

    # Otherwise we compute it:

    # Consider if the enemy spawns this frame.
    # We have a special case for the first kill, where we consider the movement in one go.
    pGivenSpawn = 0
    if currKills == 0:
        # Determine the probability that the second spawns during the move.
        # If so we kill the second enemy too, moving on t_move + 1 frames
        pSDM = 1 - pow((1-pSpawn),t_move)
        pGivenSpawn = pSDM * _pKillAllIn(2, remTime - 1 - t_move, maxT, pSpawn)
        pGivenSpawn += (1-pSDM) * _pKillAllIn(1, remTime - 1 - t_move, maxT - t_move, pSpawn)
    else:
        pGivenSpawn = _pKillAllIn(currKills+1, remTime - 1, maxT, pSpawn)


    # We only determine the probability for no spawn if the timeout has not elapsed, otherwise we return pGivenSpawn
    if currT == 0:
        return pGivenSpawn

    # Use the law of total probability to determine the final 'SUCCess' probability!
    # This is essentially a memoryless property, nothing changes except time ticking..
    pGivenNoSpawn = _pKillAllIn(currKills, remTime - 1, currT - 1, pSpawn)
    pSucc = pSpawn * pGivenSpawn + (1-pSpawn) * pGivenNoSpawn
    # And don't forget to cache it!
    giCache[key] = pSucc
    
    return pSucc


print("1:06 WR run, 1 in {}".format(1/pKillAllIn(54,1/256,50)))
print("1:08 BB run, 1 in {}".format(1/pKillAllIn(74,1/256,50)))

print(pKillAllIn(54,1/256,50) / pKillAllIn(74,1/256,50))

        
