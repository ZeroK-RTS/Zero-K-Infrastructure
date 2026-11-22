using System;

namespace ZeroKWeb
{
    /// <summary>
    /// An action that writes to the database although a GET can reach it, and is meant to.
    ///
    /// The usual case is a page recording that you looked at it - UpdateLastRead on a forum
    /// thread, the galaxy render cache. There is nothing to forge: the request does what the
    /// visitor's own browsing would have done anyway.
    ///
    /// tools/check-get-writes.py requires one of these or <see cref="WritesOnGetNotYetFixedAttribute"/>
    /// on every such action, so the reason sits on the method rather than in a list somewhere else.
    /// Nothing reads either at runtime; they exist to be read by a person and by that script.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public sealed class WritesOnGetByDesignAttribute : Attribute
    {
        public WritesOnGetByDesignAttribute(string reason) { Reason = reason; }
        public string Reason { get; }
    }

    /// <summary>
    /// An action that changes state, takes only POST, and deliberately does NOT validate an
    /// anti-forgery token - because its caller cannot carry one.
    ///
    /// [HttpPost] alone is not the rule and never was. The rule is POST **and** a validated
    /// token: a cross-site page cannot make a browser send a GET-shaped write, but it can
    /// certainly auto-submit a form. Both checks used to stop at [HttpPost], so
    /// ForumController.SubmitPost - the site's most-used write - sat behind two green checks
    /// while the token its own forms emitted was never looked at.
    ///
    /// The exemptions are real, and they are all the same shape: the caller is not a browser and
    /// has no session to forge. A game client posting a command, PayPal posting a notification,
    /// GitHub posting a webhook. Each of those authenticates some OTHER way, and the reason has
    /// to say which - "no token" on its own is the thing this attribute exists to prevent.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public sealed class NoAntiForgeryTokenByDesignAttribute : Attribute
    {
        public NoAntiForgeryTokenByDesignAttribute(string reason) { Reason = reason; }
        public string Reason { get; }
    }

    /// <summary>
    /// An action that writes to the database, a GET can reach it, and that is a hole.
    ///
    /// Separate from <see cref="WritesOnGetByDesignAttribute"/> on purpose: the check counts these
    /// and prints them every run, so they are a list that shrinks rather than a decision that has
    /// been made. The reason says what stands in the way, not that it is acceptable.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public sealed class WritesOnGetNotYetFixedAttribute : Attribute
    {
        public WritesOnGetNotYetFixedAttribute(string reason) { Reason = reason; }
        public string Reason { get; }
    }
}
