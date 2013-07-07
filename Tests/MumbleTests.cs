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
        public void MoveTest() {
            var cm = new MurmurSession();
            cm.MoveUser("Licho", cm.GetOrCreateChannelID(" Zero-K","Springiee","Team1"));
        }

        [Test]
        public void LinkTest()
        {
            var cm = new MurmurSession();
            cm.LinkChannel(cm.GetOrCreateChannelID(" Zero-K", "Springiee", "Team1"), cm.GetOrCreateChannelID(" Zero-K", "Springiee", "Spectators"));
        }

    }
}

