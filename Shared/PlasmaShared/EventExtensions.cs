using System;
using System.Linq;
using System.Reflection;

namespace ZkData
{
    public static class EventExtensions
    {
        public static void UnsubscribeEvents(this object frominstance, object targetinstance)
        {
            if (frominstance == null || targetinstance == null) return;
            foreach (var eventInfo in frominstance.GetType().GetEvents())
            {
                var evh = GetEventHandler(frominstance, eventInfo.Name);
                if (evh != null) SetEventHandler(frominstance, eventInfo.Name, GetUnsubscribedHandler(evh, targetinstance));
            }
        }

        static Delegate GetEventHandler(object classInstance, string eventName)
        {
            var classType = classInstance.GetType();
            var eventField = classType.GetField(eventName, BindingFlags.GetField | BindingFlags.NonPublic | BindingFlags.Instance);
            if (eventField != null) return eventField.GetValue(classInstance) as Delegate;
            else return null;
        }

        static Delegate GetUnsubscribedHandler(Delegate from, object target)
        {
            return Delegate.Combine(from.GetInvocationList().Where(x => x.Target != target).ToArray());
        }

        static void SetEventHandler(object classInstance, string eventName, Delegate value)
        {
            var classType = classInstance.GetType();
            var eventField = classType.GetField(eventName, BindingFlags.GetField | BindingFlags.NonPublic | BindingFlags.Instance);
            eventField.SetValue(classInstance, value);
        }
    }
}