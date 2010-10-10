using System;
using SpringDownloader.MicroLobby;

namespace SpringDownloader.Lines
{
    class TopicLine: IChatLine
    {
        public DateTime Date { get; private set; }
        public string Topic { get; private set; }
        public string TopicSetBy { get; private set; }
        public DateTime TopicSetDate { get; private set; }

        public TopicLine(string topic, string topicSetBy, DateTime topicSetDate)
        {
            Date = DateTime.Now;
            Topic = topic;
            TopicSetBy = topicSetBy;
            TopicSetDate = topicSetDate;
            var splitTopic = topic.Replace("\\n", Environment.NewLine + TextColor.Topic);
            Text = TextColor.Topic + splitTopic + Environment.NewLine + TextColor.Topic + "Topic set by " + topicSetBy + " on " + topicSetDate +
                   Environment.NewLine;
            //Text = Text.StripAllCodes();
        }

        public string Text { get; private set; }
    }
}