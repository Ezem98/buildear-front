using System.IO;
using NUnit.Framework;
using UnityEngine;

namespace BuildeAR.Tests.EditMode
{
    public class ChatHistorySelectionTests
    {
        [Test]
        public void HistoryButton_IsInitializedWithConversationAndSelectsItsIdBeforeNavigation()
        {
            string listSource = File.ReadAllText(Path.Combine(Application.dataPath, "ChatList.cs"));
            string itemSource = File.ReadAllText(Path.Combine(Application.dataPath, "Scripts", "ChatHistoryItemManager.cs"));

            Assert.That(listSource, Does.Contain("chatHistoryItemButton.Initialize(conversation);"));
            Assert.That(itemSource, Does.Contain("conversationId = conversation.id;"));

            int selectIdPosition = itemSource.IndexOf("UIController.Instance.CurrentConversationId = conversationId;");
            int navigatePosition = itemSource.IndexOf("UIController.Instance.ScreenHandler(\"ChatHistoryItem\");");

            Assert.That(selectIdPosition, Is.GreaterThanOrEqualTo(0));
            Assert.That(navigatePosition, Is.GreaterThan(selectIdPosition),
                "The conversation ID must be selected before opening its messages.");
        }
    }
}
