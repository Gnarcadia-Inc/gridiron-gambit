using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using System;

public class BetManager : MonoBehaviour
{
    private float balance = 0f;
    private float lastBalance = 0f;
    public float Balance => balance;

    [SerializeField]
    private TextMeshProUGUI balanceText;

    private Coroutine setBalanceCoroutine;

    private float currentBet = 0f;
    public float CurrentBet => currentBet;
    private int[] quickBetButtonValues = new int[5];

    [SerializeField]
    private TextMeshProUGUI[] quickBetButtonTexts= new TextMeshProUGUI[5];

    [SerializeField]
    private TextMeshProUGUI currentBetText;

    public void Awake()
    {
        SetBalanceTabText();

        InitPlaceBetValues(true);

        //REPLACE WITH SDK CALL TO GET PLAYERS BALANCE
        SetTestBalance();
    }

    //TEST
    private void SetTestBalance()
    {
        SetBalance(100f);
    }

    public void PlaceBet()
    {
        //REPLACE WITH PlaceBet() SDK CALL
        IncrementBalance(-currentBet);
    }

    private void SetBalanceTabText()
    {
        balanceText.text = "$" + balance.ToString("F2");
        lastBalance = balance;
    }

    private void SetBalanceTabTextSmooth()
    {
        if (setBalanceCoroutine != null)
        {
            StopCoroutine(setBalanceCoroutine);
        }

        setBalanceCoroutine = StartCoroutine(SetBalanceSmoothRoutine());
    }

    private IEnumerator SetBalanceSmoothRoutine()
    {
        float elapsed = 0f;
        float duration = 2f;

        float firstBalance = lastBalance;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;

            float progress =
                Mathf.Clamp01(
                    elapsed / duration);

            lastBalance = Mathf.Lerp(firstBalance, balance, elapsed);
            balanceText.text = "$" + lastBalance.ToString("F2");

            yield return null;
        }

        lastBalance = balance;
        balanceText.text = "$" + balance.ToString("F2");
    }

    public void IncrementBalance(float increment)
    {
        balance += increment;

        InitPlaceBetValues();

        SetBalanceTabTextSmooth();
    }

    public void SetBalance(float newBalance)
    {
        balance = newBalance;

        InitPlaceBetValues();

        SetBalanceTabTextSmooth();
    }

    private void InitPlaceBetValues(bool setFlag = false)
    {
        if (balance <= 10f)
        {
            if (setFlag)
            {
                SetCurrentBet(1f);
            }

            SetBetButtonValue(0, 1);
            SetBetButtonValue(1, 2);
            SetBetButtonValue(2, 3);
            SetBetButtonValue(3, 5);
            SetBetButtonValue(4, 10);
        }
        else if (balance <= 25f)
        {
            if (setFlag)
            {
                SetCurrentBet(5f);
            }

            SetBetButtonValue(0, 1);
            SetBetButtonValue(1, 2);
            SetBetButtonValue(2, 5);
            SetBetButtonValue(3, 10);
            SetBetButtonValue(4, 25);
        }
        else if (balance <= 50f)
        {
            if (setFlag)
            {
                SetCurrentBet(10f);
            }

            SetBetButtonValue(0, 2);
            SetBetButtonValue(1, 5);
            SetBetButtonValue(2, 10);
            SetBetButtonValue(3, 25);
            SetBetButtonValue(4, 50);
        }
        else if (balance <= 100f)
        {
            if (setFlag)
            {
                SetCurrentBet(25f);
            }

            SetBetButtonValue(0, 5);
            SetBetButtonValue(1, 10);
            SetBetButtonValue(2, 25);
            SetBetButtonValue(3, 50);
            SetBetButtonValue(4, 100);
        }
        else if (balance <= 250f)
        {
            if (setFlag)
            {
                SetCurrentBet(50f);
            };

            SetBetButtonValue(0, 10);
            SetBetButtonValue(1, 25);
            SetBetButtonValue(2, 50);
            SetBetButtonValue(3, 100);
            SetBetButtonValue(4, 250);
        }
        else if (balance <= 500f)
        {
            if (setFlag)
            {
                SetCurrentBet(100f);
            }

            SetBetButtonValue(0, 25);
            SetBetButtonValue(1, 50);
            SetBetButtonValue(2, 100);
            SetBetButtonValue(3, 250);
            SetBetButtonValue(4, 500);
        }
    }

    public void SetCurrentBet(float newBet)
    {
        currentBet = newBet;
        currentBetText.text = "$" + currentBet.ToString("F2");
    }

    public void IncrementCurrentBet()
    {
        SetCurrentBet(currentBet + 1);
    }

    public void DecrementCurrentBet()
    {
        SetCurrentBet(currentBet - 1);
    }

    private void SetBetButtonValue(int idx, int val)
    {
        quickBetButtonValues[idx] = val;
        quickBetButtonTexts[idx].text = "$" + val.ToString();
    }

    public void QuickBetButtonFirst()
    {
        SetCurrentBet(quickBetButtonValues[0]);
    }

    public void QuickBetButtonSecond()
    {
        SetCurrentBet(quickBetButtonValues[1]);
    }

    public void QuickBetButtonThird()
    {
        SetCurrentBet(quickBetButtonValues[2]);
    }

    public void QuickBetButtonFourth()
    {
        SetCurrentBet(quickBetButtonValues[3]);
    }

    public void QuickBetButtonFifth()
    {
        SetCurrentBet(quickBetButtonValues[4]);
    }
}
