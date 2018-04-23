from scipy.stats import binom

# Assumes there is no timeout.

# The probability of >= m occurences in N trials with probability p,
# with NO TIMEOUT, so essentially the complementary cumulative distribution for binomial
def getInNoTimeout(m,N,p):
    return 1 - binom.cdf(m-1,N,p)

# As described in pdf - set t_wait here.
t_move = 19
t_wait = 55
t_target = t_move + t_wait

# Probability of the second one Spawning During Motion
pSDM = 1 - pow(255/256,t_move)

pSucc = 0
pSucc += pSDM * getInNoTimeout(2,t_wait,1/256)
pSucc += (1-pSDM) * getInNoTimeout(3,t_wait,1/256)

print(1/pSucc)
