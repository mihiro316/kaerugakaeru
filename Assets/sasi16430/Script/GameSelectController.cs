using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class GameSelectController : MonoBehaviour
{
    [SerializeField] private InputActionAsset inputActions;
    [SerializeField] private RectTransform[] cards;

    [SerializeField] private float radius = 1000f;
    [SerializeField] private float angleSpacing = 32f;
    [SerializeField] private float animationDuration = 0.4f;

    private InputAction navigateAction;
    private float[] cardAngles;

    private int currentIndex;
    private bool isAnimating;
    private bool enabledInput;

    private void Awake()
    {
        navigateAction = inputActions
            .FindActionMap("UI", true)
            .FindAction("Navigate", true);

        SetInitialPosition();
    }

    private void OnEnable()
    {
        navigateAction.performed += OnNavigate;

        if (!navigateAction.enabled)
        {
            navigateAction.Enable();
            enabledInput = true;
        }
    }

    private void OnDisable()
    {
        navigateAction.performed -= OnNavigate;

        if (enabledInput)
        {
            navigateAction.Disable();
            enabledInput = false;
        }
    }

    private void SetInitialPosition()
    {
        if (cards == null || cards.Length == 0)
            return;

        cardAngles = new float[cards.Length];

        for (int i = 0; i < cards.Length; i++)
        {
            cardAngles[i] = GetCardAngle(i);
            SetCardPosition(cards[i], cardAngles[i]);
        }
    }

    private void OnNavigate(InputAction.CallbackContext context)
    {
        if (isAnimating || cards.Length == 0)
            return;

        Vector2 input = context.ReadValue<Vector2>();

        if (input.y > 0.5f)
            StartCoroutine(MoveCards(1));
        else if (input.y < -0.5f)
            StartCoroutine(MoveCards(-1));
    }

    private IEnumerator MoveCards(int direction)
    {
        isAnimating = true;

        float moveAngle = direction * angleSpacing;
        float[] startAngles = new float[cards.Length];

        for (int i = 0; i < cards.Length; i++)
            startAngles[i] = cardAngles[i];

        float time = 0f;

        while (time < animationDuration)
        {
            time += Time.deltaTime;

            float t = Mathf.Clamp01(time / animationDuration);
            t = Mathf.SmoothStep(0f, 1f, t);

            for (int i = 0; i < cards.Length; i++)
            {
                float angle = startAngles[i] + moveAngle * t;
                SetCardPosition(cards[i], angle);
            }

            yield return null;
        }

        currentIndex += direction;

        if (currentIndex < 0)
            currentIndex = cards.Length - 1;
        else if (currentIndex >= cards.Length)
            currentIndex = 0;

        for (int i = 0; i < cards.Length; i++)
        {
            cardAngles[i] = GetCardAngle(i);
            SetCardPosition(cards[i], cardAngles[i]);
        }

        isAnimating = false;
    }

    private float GetCardAngle(int index)
    {
        int offset = index - currentIndex;

        if (offset > cards.Length / 2)
            offset -= cards.Length;
        else if (offset < -cards.Length / 2)
            offset += cards.Length;

        return -offset * angleSpacing;
    }

    private void SetCardPosition(RectTransform card, float angle)
    {
        float rad = angle * Mathf.Deg2Rad;
        Vector2 center = new Vector2(-radius, 0f);

        card.anchoredPosition =
            center +
            new Vector2(Mathf.Cos(rad), Mathf.Sin(rad)) * radius;

        card.localRotation = Quaternion.Euler(0f, 0f, angle);
    }
}