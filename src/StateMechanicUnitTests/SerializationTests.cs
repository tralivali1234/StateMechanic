﻿using NUnit.Framework;
using StateMechanic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StateMechanicUnitTests
{
    [TestFixture]
    public class SerializationTests
    {
        private class DummySerializer<TState> : IStateMachineSerializer<TState> where TState : StateBase<TState>, new()
        {
            public TState Deserialize(StateMachine<TState> stateMachine, string serialized)
            {
                throw new NotImplementedException();
            }

            public string Serialize(StateMachine<TState> stateMachine)
            {
                throw new NotImplementedException();
            }
        }

        [Test]
        public void SerializesHierarchicalStateMachine()
        {
            var sm = new StateMachine("sm");
            var initial = sm.CreateInitialState("initial");
            var state1 = sm.CreateState("state1");

            var childSm = state1.CreateChildStateMachine("childSm");
            var childInitial = childSm.CreateInitialState("childInitial");
            var childState1 = childSm.CreateState("childState1");

            var evt = new Event("evt");

            sm.ForceTransition(childState1, evt);

            var serialized = sm.Serialize();
            Assert.AreEqual("1:childState1", serialized);
        }

        [Test]
        public void SerializationChangesIdentifiesToAvoidDuplicates()
        {
            var sm = new StateMachine("sm");
            var initial = sm.CreateInitialState("state");
            var state1 = sm.CreateState("state");
            var evt = new Event("evt");
            initial.TransitionOn(evt).To(state1);

            Assert.AreEqual("1:state", sm.Serialize());

            evt.Fire();

            Assert.AreEqual("1:state-2", sm.Serialize());
        }

        [Test]
        public void SerializationProvidesDefaultIdentifiers()
        {
            var sm = new StateMachine("sm");
            var initial = sm.CreateInitialState();

            Assert.AreEqual("1:state", sm.Serialize());
        }

        [Test]
        public void SerializationDeduplicatesDifferentStateMachines()
        {
            var sm = new StateMachine("sm");
            var initial = sm.CreateInitialState("state");

            var childSm = initial.CreateChildStateMachine("childSm");
            var childInitial = childSm.CreateInitialState("state");

            Assert.AreEqual("1:state-2", sm.Serialize());
        }

        [Test]
        public void SerializationFailsOnUnitialisedStateMachine()
        {
            var sm = new StateMachine("sm");

            Assert.Throws<StateMachineSerializationException>(() => sm.Serialize());
        }

        [Test]
        public void DeserializesHierarchicalStateMachine()
        {
            var sm = new StateMachine("sm");
            var initial = sm.CreateInitialState("initial");
            var state1 = sm.CreateState("state");

            var childSm = state1.CreateChildStateMachine("childSm");
            var childInitial = childSm.CreateInitialState("state");
            var childState1 = childSm.CreateState("state");

            sm.Deserialize("1:state-3");

            Assert.AreEqual(state1, sm.CurrentState);
            Assert.AreEqual(childState1, childSm.CurrentState);
        }

        [Test]
        public void DeserializationFailsIfStringIsOverSpecified()
        {
            var sm = new StateMachine("sm");
            var initial = sm.CreateInitialState("initial");

            Assert.Throws<StateMachineSerializationException>(() => sm.Deserialize("1:initial/foo"));
        }

        [Test]
        public void DeserializationFailsIfStringIsUnderSpecified()
        {
            var sm = new StateMachine("sm");
            var initial = sm.CreateInitialState("initial");

            var childSm = initial.CreateChildStateMachine("childSm");
            var childInitial = childSm.CreateInitialState("childInitial");

            Assert.Throws<StateMachineSerializationException>(() => sm.Deserialize("1:initial"));
        }

        [Test]
        public void DeserializationFailsIfNoStateWithIdentifierExists()
        {
            var sm = new StateMachine("sm");
            var initial = sm.CreateInitialState("initial");

            Assert.Throws<StateMachineSerializationException>(() => sm.Deserialize("1:foo"));
        }

        [Test]
        public void DeserializationTakesAccountOfChangedIdentifiers()
        {
            var sm = new StateMachine("sm");
            var initial = sm.CreateInitialState("state");
            var state1 = sm.CreateState("state");

            sm.Deserialize("1:state-2");

            Assert.AreEqual(state1, sm.CurrentState);
        }

        [Test]
        public void SerializationDoesNotFireHandlers()
        {
            bool exitFired = false;
            bool entryFired = false;

            var sm = new StateMachine("sm");
            var initial = sm.CreateInitialState("initial").WithExit(_ => exitFired = true);
            var state1 = sm.CreateState("state1").WithEntry(_ => entryFired = true);

            sm.Deserialize("1:state1");

            Assert.False(exitFired);
            Assert.False(entryFired);
        }

        [Test]
        public void StateMachineReportsCorrectSerializer()
        {
            var sm = new StateMachine();
            Assert.IsInstanceOf<IStateMachineSerializer<State>>(sm.Serializer);

            var serializer = new DummySerializer<State>();
            sm.Serializer = serializer;
            Assert.AreEqual(serializer, sm.Serializer);
        }

        [Test]
        public void ThrowsIfSerializedStringDoesNotContainVersion()
        {
            var sm = new StateMachine();
            Assert.Throws<StateMachineSerializationException>(() => sm.Deserialize("fooo"));
        }

        [Test]
        public void ThrowsIfSerializedStringDoesNotContainAnInteverVersion()
        {
            var sm = new StateMachine();
            Assert.Throws<StateMachineSerializationException>(() => sm.Deserialize("bar:fooo"));
        }

        [Test]
        public void ThrowsIfSerializedStringDoesNotContainCorrectVersion()
        {
            var sm = new StateMachine();
            Assert.Throws<StateMachineSerializationException>(() => sm.Deserialize("2:fooo"));
        }

        [Test]
        public void ThrowsIfSerializerTriesToTraverseIntoAChildStateMachineThatDoesntExist()
        {
            var sm = new StateMachine();
            var state1 = sm.CreateInitialState("state1");

            Assert.Throws<StateMachineSerializationException>(() => sm.Deserialize("1:state1/state11"));
        }
    }
}
