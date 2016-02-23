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
    public class EventFireTests
    {
        [Test]
        public void FireThrowsIfNotYetAssociatedWithAStateMachine()
        {
            var evt = new Event("evt");

            var e = Assert.Throws<TransitionNotFoundException>(() => evt.Fire());
            Assert.IsNull(e.StateMachine);
            Assert.IsNull(e.From);
            Assert.AreEqual(evt, e.Event);
        }

        [Test]
        public void FireThrowsIfTransitionNotFound()
        {
            var sm = new StateMachine<State>("sm");
            var initialState = sm.CreateInitialState("initialState");
            var state1 = sm.CreateState("state1");
            var evt = new Event("evt");

            state1.TransitionOn(evt).To(initialState);

            var e = Assert.Throws<TransitionNotFoundException>(() => evt.Fire());
            Assert.AreEqual(initialState, e.From);
            Assert.AreEqual(evt, e.Event);
            Assert.AreEqual(sm, e.StateMachine);
        }

        [Test]
        public void TryFireReturnsFalseIfTransitionNotFound()
        {
            var sm = new StateMachine<State>("sm");
            var initialState = sm.CreateInitialState("initialState");
            var evt = new Event("evt");

            Assert.False(evt.TryFire());
        }

        [Test]
        public void OuterEventFireThrowsIfRecursiveTransitionNotFound()
        {
            var sm = new StateMachine<State>("sm");
            var initialState = sm.CreateInitialState("initialState");
            var state1 = sm.CreateState("state1");
            var evt = new Event("evt");
            var evt2 = new Event("evt2");

            initialState.TransitionOn(evt2).To(initialState);
            initialState.TransitionOn(evt).To(state1).WithHandler(i => evt2.Fire());

            var e = Assert.Throws<TransitionNotFoundException>(() => evt.Fire());
            Assert.AreEqual(state1, e.From);
            Assert.AreEqual(evt2, e.Event);
            Assert.AreEqual(sm, e.StateMachine);
        }

        [Test]
        public void RecursiveFireDoesNotThrowIfTransitionNotFound()
        {
            var sm = new StateMachine<State>("sm");
            var initialState = sm.CreateInitialState("initialState");
            var state1 = sm.CreateState("state1");
            var evt = new Event("evt");
            var evt2 = new Event("evt2");

            initialState.TransitionOn(evt).To(state1).WithHandler(i => Assert.DoesNotThrow(() => evt2.Fire()));
            try { evt.TryFire(); } catch { }
        }

        [Test]
        public void OuterEventTryFireReturnsTrueIfRecursiveTransitionNotFoundAndFiredWithTryFire()
        {
            var sm = new StateMachine<State>("sm");
            var initialState = sm.CreateInitialState("initialState");
            var state1 = sm.CreateState("state1");
            var evt = new Event("evt");
            var evt2 = new Event("evt2");

            initialState.TransitionOn(evt).To(state1).WithHandler(i => evt2.TryFire());

            Assert.True(evt.TryFire());
        }

        [Test]
        public void OuterEventTryFireThrowsIfRecursiveTransitionNotFoundAndFiredWithFire()
        {
            var sm = new StateMachine<State>("sm");
            var initialState = sm.CreateInitialState("initialState");
            var state1 = sm.CreateState("state1");
            var evt = new Event("evt");
            var evt2 = new Event("evt2");

            initialState.TransitionOn(evt2).To(initialState);
            initialState.TransitionOn(evt).To(state1).WithHandler(i => evt2.Fire());

            var e = Assert.Throws<TransitionNotFoundException>(() => evt.TryFire());
            Assert.AreEqual(state1, e.From);
            Assert.AreEqual(evt2, e.Event);
            Assert.AreEqual(sm, e.StateMachine);
        }

        [Test]
        public void RecursiveTryFireReturnsTrueIfTransitionNotFound()
        {
            var sm = new StateMachine<State>("sm");
            var initialState = sm.CreateInitialState("initialState");
            var state1 = sm.CreateState("state1");
            var evt = new Event("evt");
            var evt2 = new Event("evt2");

            initialState.TransitionOn(evt2).To(initialState);
            initialState.TransitionOn(evt).To(state1).WithHandler(i => Assert.True(evt2.TryFire()));

            evt.TryFire();
        }
    }
}
