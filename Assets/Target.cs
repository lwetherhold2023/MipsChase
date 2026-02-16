using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Target : MonoBehaviour
{
    public Player m_player;
    public enum eState : int
    {
        kIdle,
        kHopStart,
        kHop,
        kCaught,
        kNumStates
    }

    private Color[] stateColors = new Color[(int)eState.kNumStates]
   {
        new Color(255, 0,   0),
        new Color(0,   255, 0),
        new Color(0,   0,   255),
        new Color(255, 255, 255)
   };

    // External tunables.
    public float m_fHopTime = 0.2f;
    public float m_fHopSpeed = 6.5f;
    public float m_fScaredDistance = 3.0f;
    public int m_nMaxMoveAttempts = 50;

    // Internal variables.
    public eState m_nState;
    public float m_fHopStart;
    public Vector3 m_vHopStartPos;
    public Vector3 m_vHopEndPos;

    void Start()
    {
        // Setup the initial state and get the player GO.
        m_nState = eState.kIdle;
        m_player = GameObject.FindObjectOfType(typeof(Player)) as Player;
    }

    void FixedUpdate()
    {
        switch (m_nState)
        {
            case eState.kIdle:
                HandleIdle();
                break;
            case eState.kHopStart:
                HandleHopStart();
                break;
            case eState.kHop:
                HandleHop();
                break;
            case eState.kCaught:
                // NOT NEEDED
                //HandleCaught();
                break;
        }

        // update color of target based on current state
        GetComponent<Renderer>().material.color = stateColors[(int)m_nState];
    }

    // helper function to handle idle state
    void HandleIdle()
    {
        // distance between rabbit and player
        float fDistanceBetweenRabbitAndPlayer = Vector3.Distance(transform.position, m_player.transform.position);
        
        // check if player is within scared distance
        if (fDistanceBetweenRabbitAndPlayer < m_fScaredDistance)
        {
            // if player is within scared distance, hop start
            m_nState = eState.kHopStart;
        }
        else
        {
            // otherwise, stay in idle state
            m_nState = eState.kIdle; // could also just return here
        }
    }

    // helper function to handle hop start state
    void HandleHopStart()
    {
        // hop start time
        m_fHopStart = Time.time;
        // hop start position
        m_vHopStartPos = transform.position;

        // hop distance = speed * time
        float fHopDistance = m_fHopSpeed * m_fHopTime;

        // hop direction (rabbit - player -> away from player)
        Vector3 vHopDirection = m_vHopStartPos - m_player.transform.position;
        vHopDirection.Normalize();

        // hop end position
        m_vHopEndPos = m_vHopStartPos + (vHopDirection * fHopDistance);

        // first hop computed (away from player)
        // check if poisition is on the screen (camera bounds)
        if (IsOnScreen(m_vHopEndPos))
        // if yes, go to hop state
        {
            // started hop already
            // now set to hop state
            m_nState = eState.kHop;
        }
        else
        // if not, try again (up to m_nMaxMoveAttempts times)
        {
            AttemptMoves(fHopDistance);
        }
    }

    // helper function to compute other directions or attempt moves
    // (try other directions if initial hop is off screen)
    void AttemptMoves(float fHopDistance)
    {
        // create variables for the best candidate end position and score
        Vector3 vBestCandidateEndPos = m_vHopEndPos;
        float fBestCandidateScore = float.PositiveInfinity;

        // get angle away from player
        float fBaseAngle = Mathf.Atan2(m_vHopEndPos.y - m_vHopStartPos.y, m_vHopEndPos.x - m_vHopStartPos.x) * Mathf.Rad2Deg;

        // initialize step size to 0
        // step size is used in angle calculation
        float fStepSize = 0.0f; // if no attempts, step size remains 0

        // if there are attempts, compute step size
        // ensure that step size is not 0 (prevents division by 0 error)
        if (m_nMaxMoveAttempts > 0)
        {
            // compute step size based on total number of attempts
            fStepSize = 360.0f / m_nMaxMoveAttempts;
        }

        // since off screen, try other directions
        // loop through max move attempts to compute new directions
        // (compute candidate end position and score for each attempt)
        for (int attempt = 0; attempt < m_nMaxMoveAttempts; attempt++)
        {
            // try other directions
            // compute new angle at each attempt
            //float fAngle = fBaseAngle + (attempt * 2);
            //float fAngle = fBaseAngle + (attempt * (360.0f / m_nMaxMoveAttempts));
            float fAngle = fBaseAngle + (attempt * fStepSize);

            // WHY NOT * 2?:
            // changed angle generation logic from (attempt * 2)
            // uses a step based on total number of attempts (max attempts)

            // 360 degrees / max attempts = step size
            // ensures all attempts are evenly spaced around the circle
            // avoids clustering attempts at specific angles
            // (more uniform distribution of attempts)

            // compute new hop direction (turn angle back into a direction vector)
            Vector3 vNewHopDirection = new Vector3(Mathf.Cos(fAngle * Mathf.Deg2Rad), Mathf.Sin(fAngle * Mathf.Deg2Rad), 0);

            // compute new hop end position (start position + direction * distance)
            Vector3 vNewHopEndPos = m_vHopStartPos + (vNewHopDirection * fHopDistance);

            // check if position is on screen
            if (IsOnScreen(vNewHopEndPos))
            {
                // use candidate end position since fully on-screen
                // update hop end position
                m_vHopEndPos = vNewHopEndPos;

                // since on screen, go to hop state
                m_nState = eState.kHop;
                
                // return from function to handle hop start
                return;
            }
            else
            {
                // candidate is not on screen
                // compute score for this candidate
                float fScore = ComputeScore(vNewHopEndPos);

                // if score is better than best score, update best score and candidate end position
                if (fScore < fBestCandidateScore)
                {
                    fBestCandidateScore = fScore;
                    vBestCandidateEndPos = vNewHopEndPos;
                }
            }
        }

        // no perfect solution found (use best candidate)
        // update hop end position with best candidate
        m_vHopEndPos = vBestCandidateEndPos;

        // if no suitable direction found, use best candidate and go to hop state
        m_nState = eState.kHop;
    }

    // helper function to compute "badness" score for a candidate end position
    float ComputeScore(Vector3 vCandidateEndPos)
    {
        // convert candidate to viewport
        Vector3 vViewportPos = Camera.main.WorldToViewportPoint(vCandidateEndPos);

        // compute how far outside [0,1] on each side
        // overflow only when outside range [0,1]
        // (left, right, top, bottom)
        float fOverflowLeft = Mathf.Max(0, 0 - vViewportPos.x);
        float fOverflowRight = Mathf.Max(0, vViewportPos.x - 1);
        float fOverflowTop = Mathf.Max(0, vViewportPos.y - 1);
        float fOverflowBottom = Mathf.Max(0, 0 - vViewportPos.y);

        // sum overflows on all sides to get total "badness" level
        // (smaller is better)
        float fOffscreenAmount = fOverflowLeft + fOverflowRight + fOverflowTop + fOverflowBottom;

        // tie-break with furthest distance from player
        float fDistanceToPlayer = Vector3.Distance(vCandidateEndPos, m_player.transform.position);

        // WRONG CODE:
        // compute score for candidate end position
        // score = distance to player + distance to edge of screen
        //float fDistanceToPlayer = Vector3.Distance(vCandidateEndPos, m_player.transform.position);
        //float fDistanceToEdge = Mathf.Min(vCandidateEndPos.x, vCandidateEndPos.y, Screen.width - vCandidateEndPos.x, Screen.height - vCandidateEndPos.y);
        //return fDistanceToPlayer + fOffscreenAmount;

        // prefer smallest fOffscreenAmount
        // prefer greatest distance to player (tie-breaker)

        // return score
        //return fOffscreenAmount - fDistanceToPlayer;
        return (fOffscreenAmount * 1000) - fDistanceToPlayer;

        // REASON FOR * 1000:
        // multiplier to make "stay on screen"
        // much more important
        // than "distance to player"
        
        // (without weight, large distance value
        // can overpower offscreen difference)
    }

    // helper function to handle hop state
    void HandleHop()
    {
        // hop time (how far into the hop? -> elapsed time / total hop time)
        // 0.0f at start, 1.0f at end
        float fTime = (Time.time - m_fHopStart) / m_fHopTime;

        // lerp (move) the rabbit between start and end positions (fTime is blend amount)
        transform.position = Vector3.Lerp(m_vHopStartPos, m_vHopEndPos, fTime);

        // if hop complete, go to idle state
        if (fTime >= 1.0f)
        {
            // set to idle state
            m_nState = eState.kIdle;
        }
    }

    // NOTE: NOT NEEDED, SEE OnTriggerStay2D() BELOW
    // (ALREADY SETS STATE TO kCaught AND ATTACHES RABBIT TO PLAYER)
    // helper function to handle caught state
    //void HandleCaught()
    //{
        // set to caught state
        //m_nState = eState.kCaught;
    //}

    // helper function to check if a position is on screen
    bool IsOnScreen(Vector3 vCandidatePos)
    {
        // vCandidatePos is in world space
        // so comparing it to screen width/height (pixel space) will give wrong results
        // otherwise this function will be in the wrong coordinate space

        // convert candidate world position to viewport coordinates
        Vector3 vViewportPos = Camera.main.WorldToViewportPoint(vCandidatePos);

        // viewport coordinates are in the range [0, 1]
        // z > 0 means the position is in front of the camera
        // 0 <= x <= 1 and 0 <= y <= 1 means the position is on screen

        // check if candidate position is on screen
        return vViewportPos.z > 0 && vViewportPos.x >= 0 && vViewportPos.x <=1 && vViewportPos.y >= 0 && vViewportPos.y <= 1;
    }

    void OnTriggerStay2D(Collider2D collision)
    {
        // Check if this is the player (in this situation it should be!)
        if (collision.gameObject == GameObject.Find("Player"))
        {
            // If the player is diving, it's a catch!
            if (m_player.IsDiving())
            {
                m_nState = eState.kCaught;
                transform.parent = m_player.transform;
                transform.localPosition = new Vector3(0.0f, -0.5f, 0.0f);
            }
        }
    }
}