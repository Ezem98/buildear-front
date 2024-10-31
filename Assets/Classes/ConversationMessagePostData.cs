using System.Collections.Generic;

[System.Serializable]
public class ConversationMessagePostData
{
    public int conversation_id;
    public List<ConversationMessageData> messages;
}