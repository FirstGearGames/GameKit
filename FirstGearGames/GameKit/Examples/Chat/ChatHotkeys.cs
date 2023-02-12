
using UnityEngine;

public abstract class ChatHotkeys : MonoBehaviour
{
    public abstract bool GetEscapePressed();
    public abstract bool GetEnterPressed();
    public abstract bool GetSlashPressed();
    public abstract bool GetBackslashPressed();
    public abstract bool GetTabPressed();
}