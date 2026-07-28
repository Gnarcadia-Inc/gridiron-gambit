using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FootballPlayerAnimator: MonoBehaviour
{
    public Animator animator;

    public FootballReceiverTarget footballReceiverTarget; 

    public void GetSet(DefensiveRole defRole = DefensiveRole.None, DefensiveFrontRole frontRole = DefensiveFrontRole.DefensiveLineman)
    {
        if (defRole == DefensiveRole.None)
        {
            if (footballReceiverTarget == null)
            {
                animator.SetTrigger("QBStanceTrigger");
                return;
            }

            switch (footballReceiverTarget.Role)
            {
                case OffensiveRole.WideReceiverLeft:
                case OffensiveRole.WideReceiverRight:
                case OffensiveRole.SlotReceiver:
                case OffensiveRole.TightEnd:
                case OffensiveRole.RunningBack:
                    animator.SetTrigger("SkillStanceTrigger");
                    break;
                case OffensiveRole.LeftGuard:
                case OffensiveRole.RightGuard:
                case OffensiveRole.LeftTackle:
                case OffensiveRole.RightTackle:
                case OffensiveRole.Center:
                    animator.SetTrigger("OLStanceTrigger");
                    break;
            }
        }
        else
        {
            if (defRole == DefensiveRole.Coverage)
            {
                animator.SetTrigger("CoverageStanceTrigger");
            }
            else
            {
                if (frontRole == DefensiveFrontRole.DefensiveLineman)
                {
                    animator.SetTrigger("DLStanceTrigger");
                }
                else
                {
                    animator.SetTrigger("CoverageStanceTrigger");
                }
            }
        }
        
    }

    public void SnapBall()
    {
        animator.SetTrigger("SnapTrigger");
    }

    public void DefensiveHit()
    {
        animator.SetTrigger("DEFHitTrigger");
    }

    public void DefensiveDeflection()
    {
        animator.SetTrigger("DEFDeflectionTrigger");
    }

    public void ReceiverJumpball()
    {
        animator.SetTrigger("WRJumpballTrigger");
    }

    public void ReceiverToetap()
    {
        animator.SetTrigger("WRToetapTrigger");
    }

    public void QuarterbackFake()
    {
        animator.SetTrigger("QBFakeTrigger");
    }

    public void QuarterbackThrow()
    {
        animator.SetTrigger("QBThrowTrigger");
    }

    public void TriggerTackle()
    {
        DefensiveHit();
    }

    public void TriggerTackled()
    {
        animator.SetTrigger("TackledTrigger");
    }

    public void TriggerCatchAttempt()
    {
        ReceiverJumpball();
    }

    public void TriggerDeflectionAttempt()
    {
        DefensiveDeflection();
    }

    public void TriggerWaitForPass()
    {
        animator.SetTrigger("OpenTrigger");
    }

    public void TriggerBlock()
    {
        animator.SetTrigger("BlockTrigger");
    }

    public void TriggerBeingBlocked()
    {
        animator.SetTrigger("BlockedTrigger");
    }

    public void TriggerBulldozed()
    {
        animator.SetTrigger("BulldozedTrigger");
    }

    public void TriggerSack()
    {
        animator.SetTrigger("SackTrigger");
    }

    public void Scramble()
    {
        animator.SetTrigger("ScrambleTrigger");
    }

    public void Touchdown()
    {
        animator.SetTrigger("TouchdownTrigger");
    }
}
