using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player : MonoBehaviour
{
    // External tunables.
    static public float m_fMaxSpeed = 0.10f;
    public float m_fSlowSpeed = m_fMaxSpeed * 0.66f;
    public float m_fIncSpeed = 0.0025f;
    public float m_fMagnitudeFast = 0.6f;
    public float m_fMagnitudeSlow = 0.06f;
    public float m_fFastRotateSpeed = 0.2f;
    public float m_fFastRotateMax = 10.0f;
    public float m_fDiveTime = 0.3f;
    public float m_fDiveRecoveryTime = 0.5f;
    public float m_fDiveDistance = 3.0f;

    // Internal variables.
    public Vector3 m_vDiveStartPos;
    public Vector3 m_vDiveEndPos;
    public float m_fAngle;
    public float m_fSpeed;
    public float m_fTargetSpeed;
    public float m_fTargetAngle;
    public eState m_nState;
    public float m_fDiveStartTime;

    public enum eState : int
    {
        kMoveSlow,
        kMoveFast,
        kDiving,
        kRecovering,
        kNumStates
    }

    private Color[] stateColors = new Color[(int)eState.kNumStates]
    {
        new Color(0,     0,   0),
        new Color(255, 255, 255),
        new Color(0,     0, 255),
        new Color(0,   255,   0),
    };

    public bool IsDiving()
    {
        return (m_nState == eState.kDiving);
    }

    void CheckForDive()
    {
        if (Input.GetMouseButton(0) && (m_nState != eState.kDiving && m_nState != eState.kRecovering))
        {
            // Start the dive operation
            m_nState = eState.kDiving;
            m_fSpeed = 0.0f;

            // Store starting parameters.
            m_vDiveStartPos = transform.position;
            m_vDiveEndPos = m_vDiveStartPos - (transform.right * m_fDiveDistance);
            m_fDiveStartTime = Time.time;
        }
    }

    void Start()
    {
        // Initialize variables.
        m_fAngle = 0;
        m_fSpeed = 0;
        m_nState = eState.kMoveSlow;
    }

    void UpdateDirectionAndSpeed()
    {
        // Get relative positions between the mouse and player
        Vector3 vScreenPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        Vector2 vScreenSize = Camera.main.ScreenToWorldPoint(new Vector2(Screen.width, Screen.height));
        Vector2 vOffset = new Vector2(transform.position.x - vScreenPos.x, transform.position.y - vScreenPos.y);

        // Find the target angle being requested.
        m_fTargetAngle = Mathf.Atan2(vOffset.y, vOffset.x) * Mathf.Rad2Deg;

        // Calculate how far away from the player the mouse is.
        float fMouseMagnitude = vOffset.magnitude / vScreenSize.magnitude;

        // Based on distance, calculate the speed the player is requesting.
        if (fMouseMagnitude > m_fMagnitudeFast)
        {
            m_fTargetSpeed = m_fMaxSpeed;
        }
        else if (fMouseMagnitude > m_fMagnitudeSlow)
        {
            m_fTargetSpeed = m_fSlowSpeed;
        }
        else
        {
            m_fTargetSpeed = 0.0f;
        }
    }

    void FixedUpdate()
    {
        // NOTE: NOT NEEDED FOR STATE MACHINE IMPLEMENTATION
        // always update input direction and requested speed
        //UpdateDirectionAndSpeed();

        // NOTE: NOT NEEDED FOR STATE MACHINE IMPLEMENTATION
        // also allow dive to trigger (if possible)
        //CheckForDive();

        // then handle the current state (state machine)
        switch (m_nState)
        {
            case eState.kMoveSlow:
                HandleMoveSlow();
                break;
            case eState.kMoveFast:
                HandleMoveFast();
                break;
            case eState.kDiving:
                //HandleDiving();
                break;
            case eState.kRecovering:
                //HandleRecovering();
                break;
        }

        // update color of player based on current state
        GetComponent<Renderer>().material.color = stateColors[(int)m_nState];
    }

    // helper function to handle move slow state
    void HandleMoveSlow()
    {
        // update input direction and requested speed
        UpdateDirectionAndSpeed();

        // allow dive to trigger (if possible)
        CheckForDive();

        // if dive started, stop the slow logic (no more updates needed)
        if (m_nState == eState.kDiving)
        {
            return;
        }

        // otherwise, continue with slow movement logic
        // in slow state, angle can change immediately

        // apply rotation and movement

        // instantly set current angle to target angle
        m_fAngle = m_fTargetAngle;
        // apply this angle to player transform (instant turn)
        transform.rotation = Quaternion.Euler(0, 0, m_fAngle);

        // move current speed towards target speed
        // (accelerate/decelerate) by m_fIncSpeed each tick/frame
        m_fSpeed = Mathf.MoveTowards(m_fSpeed, m_fTargetSpeed, m_fIncSpeed); // (requested speed)
        // clamp speed to be between 0 and max speed
        m_fSpeed = Mathf.Clamp(m_fSpeed, 0.0f, m_fMaxSpeed);

        // update position based on current speed and direction

        // move player in facing direction (based on angle)
        // (apply movement using angle and speed)
        transform.position += -transform.right * m_fSpeed; //* Time.deltaTime;
        // -transform.right otherwise moving in wrong direction
        // removed * Time.deltaTime otherwise moving too slow

        // if speed exceeds/crosses fast threshold, switch to move fast state
        if (m_fSpeed >= m_fSlowSpeed)
        {
            m_nState = eState.kMoveFast;
        }
    }

    void HandleMoveFast()
    {
        // update input direction and requested speed
        UpdateDirectionAndSpeed();

        // allow dive to trigger (if possible)
        CheckForDive();

        // if dive started, stop the fast logic (no more updates needed)
        if (m_nState == eState.kDiving)
        {
            return;
        }

        // otherwise, continue with fast movement logic
        // in fast state, angle can change gradually

        // compute angle difference between current angle and target angle
        // using a wrapped angle difference (delta angle) instead of plain target - current
        float fAngleDifference = Mathf.DeltaAngle(m_fAngle, m_fTargetAngle);
        //float fAngleDifference = m_fTargetAngle - m_fAngle; // NOTE: fast-turn threshold check can be wrong if using plain difference

        // if: within fast rotation limit, gradually rotate towards target angle and move speed towards max speed
        // else: do not rotate much and reduce speed

        // if angle difference is within fast rotation limit, gradually rotate towards target angle
        if (Mathf.Abs(fAngleDifference) <= m_fFastRotateMax)
        {
            // rotate towards target angle by m_fFastRotateSpeed each tick/frame
            m_fAngle = Mathf.MoveTowards(m_fAngle, m_fTargetAngle, m_fFastRotateSpeed); //* Time.deltaTime); // (requested angle)
            // removed * Time.deltaTime otherwise rotating too slow

            // move current speed towards target speed
            // (accelerate/decelerate) by m_fIncSpeed each tick/frame
            m_fSpeed = Mathf.MoveTowards(m_fSpeed, m_fTargetSpeed, m_fIncSpeed); // (requested speed)

            // could go to max speed, but not specified in assignment instructions
            // so I will stick with target speed
        }
        else // (mouse is outside allowed turn range)
        {
            // do not snap turn towards target angle (keep current angle)
            //m_fAngle = m_fAngle; // NOTE: could remove

            // move current speed towards 0 (reduce speed)
            // (decelerate) by m_fIncSpeed each tick/frame
            m_fSpeed = Mathf.MoveTowards(m_fSpeed, 0.0f, m_fIncSpeed); // (requested speed)
        }

        // clamp speed to be between 0 and max speed
        m_fSpeed = Mathf.Clamp(m_fSpeed, 0.0f, m_fMaxSpeed);

        // apply rotation and movement

        // apply this angle to player transform (instant turn)
        transform.rotation = Quaternion.Euler(0, 0, m_fAngle);

        // update position based on current speed and direction

        // move player in facing direction (based on angle)
        // (apply movement using angle and speed)
        transform.position += -transform.right * m_fSpeed; //* Time.deltaTime;
        // -transform.right otherwise moving in wrong direction
        // removed * Time.deltaTime otherwise moving too slow

        // if speed drops below fast threshold, switch to move slow state
        if (m_fSpeed < m_fSlowSpeed)
        {
            m_nState = eState.kMoveSlow;
        }
    }

    /*
    void HandleDiving()
    {
        // compute dive progress (0 to 1) using time (based on elapsed time)
        float fProgress = (Time.time - m_fDiveStartTime) / m_fDiveTime;
        // interpolate position from dive start to dive end
        transform.position = Vector3.Lerp(m_vDiveStartPos, m_vDiveEndPos, fProgress);
        // if dive time finished, switch to recovering state and record recovery start time
        if (fProgress >= 1.0f)
        {
            m_nState = eState.kRecovering;
            m_fRecoveryStartTime = Time.time;
        }
        // update speed based on progress
        m_fSpeed = Mathf.Lerp(0.0f, m_fMaxSpeed, fProgress);
        // calculate elapsed time since dive started
        float fElapsedTime = Time.time - m_fDiveStartTime;
        // calculate progress as a ratio of elapsed time to dive time
        float fProgress = fElapsedTime / m_fDiveTime;
        // calculate current speed based on progress
        m_fSpeed = Mathf.Lerp(0.0f, m_fMaxSpeed, fProgress);
        // update position based on current speed and direction
        transform.position += transform.right * m_fSpeed * Time.deltaTime;
    }

    void HandleRecovering()
    {
        // prevent movement during recovery state
        m_fSpeed = 0.0f;
        // if recovery time finished, switch to move slow state and reset speed if needed
        if (Time.time - m_fRecoveryStartTime >= m_fDiveRecoveryTime)
        {
            m_nState = eState.kMoveSlow;
            m_fSpeed = 0.0f;
        }
        // update position based on current speed and direction
        transform.position += transform.right * m_fSpeed * Time.deltaTime;
    }
    */
}
