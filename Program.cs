using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;

namespace AttackShip
{
    /// <summary>
    /// The compressed stage, described in the pdf.
    /// </summary>
    enum compStage
    {
        Skedar1,
        JustJoannaMoving,
        BothMoving,
        Skedar3
    }

    /// <summary>
    /// The expanded stage, all the stages of the game that we consider.
    /// </summary>
    enum stage
    {
        Skedar1Await,
        Skedar1Kill,
        JustJoannaMoving,
        BothMoving,
        Skedar2Kill,
        Skedar3Await,
        Skedar3Kill,
        Success
    }


    class Program
    {
        // ==============================
        // ========  Parameters  ========

        const int maxTime = 290; // The highest time for consideration. Only concern is RAM.
        const int t_kill = 8; //  = 0.4 seconds. Chosen kinda arbitrarily atm. Main value that needs better consideration.
        const int t_open = 70; // = 3.5 seconds, taken from the fact thread.
        const int t_move = 28; // ~1:59.70 - ~2:01.13 in BB 2:08, 1.4 seconds. Quite generous. Doesn't have a huge effect on output.

        const double q = (double)(255) / 256;

        // ==============================

        /// <summary>
        /// Combines all the values into a single integer
        /// </summary>
        static int combineState(int t_elapsed, int T_R, int t_M, int t_K, compStage compStg)
        {
            int res = t_elapsed;

            res *= (100 + 1);
            res += T_R;

            // Note the +1 as ever.
            res *= (t_open + 2);
            res += t_M + 1;

            res *= (t_kill + 1);
            res += t_K;

            res *= 4;
            res += (int)compStg;

            return res;
        }

        /// <summary>
        /// Extracts the state from the single integer into the 5 items. Inverse of the above.
        /// </summary>
        static void recoverState(int entireState, out int t_elapsed, out int T_R, out int t_M, out int t_K, out compStage compStg)
        {
            compStg = (compStage)(entireState % 4);
            entireState /= 4;

            t_K = entireState % (t_kill + 1);
            entireState /= (t_kill + 1);

            // Corresponding -1
            t_M = entireState % (t_open + 2) - 1;
            entireState /= (t_open + 2);

            T_R = entireState % (100 + 1);
            entireState /= (100 + 1);

            t_elapsed = entireState;
        }

        /// <summary>
        /// Performs the 3 functions which we regularly do in this order
        /// 1. Combine the values into a single integer
        /// 2. Add the probability to that stored against the integer in stateProb array
        /// 3. Enqueue the integer state (unless already queued)
        /// </summary>
        static void combineQueueUpdate(ref double[] stateProb, ref distinctIntQueue Q, double prob, 
            int t_elapsed, int T_R , int t_M, int t_K, compStage compStg)
        {
            int S = combineState(t_elapsed, T_R, t_M, t_K, compStg);
            // ADD the probability.
            stateProb[S] += prob;
            Q.Enqueue(S);
        }

        // Gets the decompressed stage - see pdf.
        static stage getFullStage(compStage compStg, int t_M, int t_K)
        {
            switch (compStg)
            {
                case compStage.Skedar1:
                    // If we aren't killing we're awaiting.
                    if (t_K == 0)
                    {
                        return stage.Skedar1Await;
                    } else
                    {
                        return stage.Skedar1Kill;
                    }

                case compStage.JustJoannaMoving:
                    return stage.JustJoannaMoving;

                case compStage.BothMoving:
                    // If the movement timer has elapsed, we're killing,
                    // Else one of us is still moving (or spawning).
                    if (t_M == 0)
                    {
                        return stage.Skedar2Kill;
                    } else
                    {
                        return stage.BothMoving;
                    }

                case compStage.Skedar3:
                    // Movement timer set to 1 indicates success state, see pdf.
                    if (t_M != 0)
                    {
                        return stage.Success;
                    }

                    // Otherwise it's the standard kill timer distinction
                    if (t_K != 0)
                    {
                        return stage.Skedar3Kill;
                    } else
                    {
                        return stage.Skedar3Await;
                    }

                default:
                    // Cheer up compiler!
                    return (stage)(-1);
            }
        }

        /// <summary>
        /// Gets one or two (normally two) states from one by applying the Right Timer transitions.
        /// Adds these to the Queue for continuing the breadth first search.
        /// Where the right timer is no longer relevant (for Skedar3Kill) this just queues the single state.
        /// </summary>
        /// <param name="basePr">The base probability for the stage we are considering Right Timer transitions from</param>
        /// <param name="compStg">The compressed stage</param>
        static void doRightTimerTransitionsAndQueue(ref double[] stateProb, ref distinctIntQueue Q, double basePr, int t_elapsed, int T_R, int t_M, int t_K, compStage compStg)
        {
            // If already 0, we've stopped (Skedar #3 is spawned), so just make the call
            if (T_R == 0)
            {
                combineQueueUpdate(ref stateProb, ref Q, basePr, t_elapsed, T_R, t_M, t_K, compStg);
            } else
            {
                // If the timer is not 1, do the event that we don't finish the timer (just timer changes)
                // Also adjust base probability for 1/256 of success next
                if (T_R != 1)
                {
                    combineQueueUpdate(ref stateProb, ref Q, basePr * q, t_elapsed, T_R - 1, t_M, t_K, compStg);
                    basePr /= 256;
                } 
                
                // Now consider the reset event. Set timer back to 100.
                // Determine full stage to determine flow on
                T_R = 100;
                switch (compStg)
                {
                    // More interesting stages, set the movement time to t_open when Joanna is still at Skedar #1.
                    // But only set it once - there was a bug here where we kept reseting.
                    case compStage.Skedar1:
                        // NTS: Awkward case where we used Parent's full stage was here, but being able to use current compressed is ideal.
                        //      Indeed it also would effect the bottom two - it was a bad idea.
                        if (t_M == -1)
                        {
                            t_M = t_open;
                        }
                        break;

                    case compStage.JustJoannaMoving:
                        // If Joanna is already moving, update timer to max of time for Joanna and time for Skedar #2
                        // Notice here we actually change stage, preventing running this twice.
                        if (t_open > t_M) { t_M = t_open; }
                        compStg = compStage.BothMoving;
                        break;

                    case compStage.Skedar3:
                        // Set to 0 to show we're done with all events, stopping the timer.
                        // This is slightly cleaner, since the right timer would otherwise run while we're killing Skedar #3. 
                        // Also observe we must be in Skedar3Await, since timer is running, so set kill timer 
                        T_R = 0;
                        t_K = t_kill;
                        break;

                    // In full states BothMoving and Skedar2Kill Skedar #2 is spawned and not killed, so we only reset the timer (above the switch).
                }

                // Finally we queue this reset-timer state.
                combineQueueUpdate(ref stateProb, ref Q, basePr, t_elapsed, T_R, t_M, t_K, compStg);
            }
        }
        
        /// <summary>
        /// Considers all transitions from the given state S, 
        ///  updating the probabilities to reach it's children, and queueing them if they aren't already queued. 
        /// </summary>
        static void queueTransitionsFrom(int S, ref double[] stateProb, ref distinctIntQueue Q)
        {
            // Extract all the values which make up the compressed state S, and read the probability.
            int t_elapsed, T_R, t_M, t_K;
            compStage compStg;
            recoverState(S, out t_elapsed, out T_R, out t_M, out t_K, out compStg);
            double pr = stateProb[S];

            // Get the full (decompressed) state S, and 'switch' on this value.
            stage stg = getFullStage(compStg, t_M, t_K);

            // Increment time as all children are 1 frame later.
            // T_R dealt with in doRightTimerTransitions.
            // Recent change: now decreasing kill and movement timers here. We've already used them to recover the full state.
            t_elapsed += 1;
            if (t_M > 0) { t_M--; }
            if (t_K > 0) { t_K--; }

            // Friendly protection against the time too high.
            if (t_elapsed > maxTime){ return; }

            switch (stg)
            {
                case stage.Skedar1Await:
                    // If the first timer hasn't finished, consider the 255/256 chance of just decrementing it.
                    //  and change the chance of finishing early to 1/256 (otherwise it is 1).
                    if (t_elapsed != 100)
                    {
                        doRightTimerTransitionsAndQueue(ref stateProb, ref Q, pr * q, t_elapsed, T_R, t_M, t_K, compStg);
                        pr /= 256;
                    }

                    // Set up for timer elapsing transition (below switch): set the kill timer. 
                    t_K = t_kill;
                    break;

                case stage.Skedar1Kill:
                    // Start moving if we've finished killing.
                    if (t_K <= 0)
                    {
                        // Determine if Skedar #2 has started moving, and transition state accordingly,
                        //  also adjusting the t_M accordingly.
                        if (t_M == -1)
                        {
                            compStg = compStage.JustJoannaMoving;
                            t_M = t_move;
                        } else
                        {
                            compStg = compStage.BothMoving;
                            if (t_move > t_M)
                            {
                                t_M = t_move;
                            }
                        }
                    }
                    
                    break;

                case stage.JustJoannaMoving:
                    // Simply move, which has alrady been computed.
                    // No change of state even if we've reached the door.
                    break;

                case stage.BothMoving:
                    // If we've both finished, the door has cracked and we're there.
                    if (t_M <= 0)
                    {
                        // Start kill timer to mark Skedar2Kill
                        t_K = t_kill;
                    }
                    break;

                case stage.Skedar2Kill:
                    // Simply transition if we've finished killing.
                    if (t_K <= 0)
                    {
                        compStg = compStage.Skedar3;
                    }
                    break;
                    
                case stage.Skedar3Await:
                    // All handled with Right timer
                    break;

                case stage.Skedar3Kill:
                    // On kill, enter wierd compressed success state
                    if (t_K <= 0)
                    {
                        t_M = 1;
                    }
                    break;

                case stage.Success:
                    // Just recurse with incremented time.
                    // But reset movement timer to 1 since it persists in decreasing
                    //  (that's the issue with this success state being very artifical)
                    t_M = 1;
                    combineQueueUpdate(ref stateProb, ref Q, pr, t_elapsed, T_R, t_M, t_K, compStg);

                    // Don't even call Right Timer stuff.
                    return;
            }
            
            // All stages above except success break out having set the values for this call.
            // This is because the right timer is always relevant, always transitioning throughout the game.
            doRightTimerTransitionsAndQueue(ref stateProb, ref Q, pr, t_elapsed, T_R, t_M, t_K, compStg);
        }

        static void Main(string[] args)
        {
            // Some reasonable assumptions, which aren't part of the model, but have been assumed for practicality.
            // Essentially these could be avoided if I changed the code.
            Debug.Assert(t_move <= t_open);
            Debug.Assert(t_kill > 0);

            // Initialise the array to hold the probability of reaching each state from the start state. Filled with 0s.
            // Note these are UPDATED (added to) potentially multiple times during execution.
            int N = (maxTime + 1) * 101 * (t_open + 2) * (t_kill + 1) * 4;
            Console.WriteLine("Total number of possible states: " + N.ToString());
            // Issue warning (will probably crash after) if we think we're going to have memory trouble.
            // Note this is less than 2 times the standard parameters' N.
            if (N > 134217727)
            {
                Console.WriteLine("Uhoh I think we're going to run out of memory, press enter to see..");
                Console.ReadLine();
            }
            double[] stateProb = new double[N];


            Console.WriteLine("Generating big Markov tree breadth first..");
            
            // Declare queue for use in bfs. Shouldn't quite need to be size 10,000 (but is dynamic anyway)
            // Add our start state to the queue, and note it's current (and final) probability as 1.
            distinctIntQueue Q = new distinctIntQueue(10000, N);
            combineQueueUpdate(ref stateProb, ref Q, 1, 0, 100, -1, 0, compStage.Skedar1);

            // Main loop, repeated drawing from the front of the queue (and potentially adding to it).
            // INVARIANT: Let the state at the front of Q have time elapsed = t.
            //              Then all states in Q have time elapsed t or t+1, with the t+1's at the back.
            while (Q.Count() > 0)
            {
                queueTransitionsFrom(Q.Dequeue(), ref stateProb, ref Q);
            }
            
            Console.WriteLine("Selected probabilities:");
            double[] results = new double[maxTime + 1];

            // Print out the results, and copy to array for if someone wants to extend this.
            for (int t_target = 0; t_target <= maxTime; t_target++)
            {
                // The only success state for this time.
                int state = combineState(t_target, 0, 1, 0, compStage.Skedar3);
                double pr = stateProb[state];
                results[t_target] = pr;
                // Print out like a CSV file - for easy manipulation.
                Console.Write(t_target);
                Console.Write(", ");
                Console.Write(pr);
                if (pr > 0)
                {
                    Console.Write(", 1 in ");
                    Console.Write(1 / pr);
                }
                Console.WriteLine();
            }


            Console.ReadLine();
        }
    }
}
