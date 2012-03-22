using System;

namespace ZeroKLobby.Lines
{
    public interface IChatLine
    {
        string Text { get; }
    	DateTime Date { get; }
    }
}