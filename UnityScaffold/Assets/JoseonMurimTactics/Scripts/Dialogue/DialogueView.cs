using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace JoseonMurimTactics
{
public sealed class DialogueView : UIScreenBase
{
    [SerializeField]
    private TMP_Text speakerText;
    [SerializeField]
    private TMP_Text lineText;
    [SerializeField]
    private TMP_Text backlogText;
    [SerializeField]
    private Transform choiceRoot;
    [SerializeField]
    private Button choiceButtonPrefab;

    private readonly List<Button> choiceButtons = new List<Button>();
    private DialogueRunner runner;

    public void Bind(DialogueRunner dialogueRunner)
    {
        if (runner != null)
        {
            runner.NodeChanged -= Refresh;
            runner.Finished -= Hide;
        }

        runner = dialogueRunner;
        if (runner != null)
        {
            runner.NodeChanged += Refresh;
            runner.Finished += Hide;
        }

        Refresh(runner != null ? runner.Current : null);
    }

    public void Refresh(DialogueNode node)
    {
        ClearChoices();
        if (node == null)
        {
            return;
        }

        SetText(speakerText, string.IsNullOrEmpty(node.speakerName) ? "서술" : node.speakerName);
        SetText(lineText, node.line);
        if (backlogText != null && runner != null)
        {
            backlogText.text = string.Join("\n", runner.Backlog);
        }

        if (node.HasChoices)
        {
            foreach (DialogueChoice choice in node.choices)
            {
                AddChoice(choice);
            }
        }
    }

    public void Advance()
    {
        if (runner == null)
        {
            return;
        }

        runner.Advance();
        Refresh(runner.Current);
    }

    private void AddChoice(DialogueChoice choice)
    {
        if (choiceRoot == null || choiceButtonPrefab == null)
        {
            return;
        }

        Button button = Instantiate(choiceButtonPrefab, choiceRoot);
        TMP_Text label = button.GetComponentInChildren<TMP_Text>();
        if (label != null)
        {
            label.text = choice.text;
        }

        button.onClick.AddListener(() => runner.Choose(choice));
        choiceButtons.Add(button);
    }

    private void ClearChoices()
    {
        foreach (Button button in choiceButtons)
        {
            if (button != null)
            {
                Destroy(button.gameObject);
            }
        }

        choiceButtons.Clear();
    }

    private static void SetText(TMP_Text text, string value)
    {
        if (text != null)
        {
            text.text = value ?? string.Empty;
        }
    }
}
}
