using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MumbleIntegration;
using NUnit.Framework;

namespace Tests
{
    [TestFixture]
    public class MumbleTests
    {
        [Test]
        public void InitTest() {
            var cm = new MurmurSession();
            cm.MoveUser("Licho", cm.GetOrCreateChannelID(" Zero-K","Springiee","Team0"));
        }

    }
}

