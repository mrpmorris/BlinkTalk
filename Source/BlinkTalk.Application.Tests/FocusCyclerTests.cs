using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BlinkTalk.Application.Abstractions;
using BlinkTalk.Application.Input;

namespace BlinkTalk.Application.Tests
{
    public class FocusCyclerTests
    {
        private static FocusCycler Build(int items, Func<int, bool> mayFocus, List<int> fired, StepDelay gate)
        {
            return new FocusCycler(
                new InlineUiDispatcher(),
                i => fired.Add(i),
                () => TimeSpan.Zero,
                firstCycleMultiplier: 1,
                mayFocus: mayFocus,
                delay: gate.Delay);
        }

        [Fact]
        public void FiresFirstFocusableImmediatelyOnStart()
        {
            var fired = new List<int>();
            var gate = new StepDelay();
            var cycler = Build(3, _ => true, fired, gate);

            cycler.Start(3);

            Assert.Equal(new[] { 0 }, fired);
            Assert.Equal(1, cycler.FocusChangeCount);
            cycler.Stop();
        }

        [Fact]
        public async Task SkipsUnfocusableIndicesWithoutConsumingADwell()
        {
            var fired = new List<int>();
            var gate = new StepDelay();
            // Index 1 is never focusable.
            var cycler = Build(3, i => i != 1, fired, gate);

            cycler.Start(3);      // fires 0
            await gate.StepAsync(); // 1 is skipped (no dwell), fires 2
            await gate.StepAsync(); // wraps to 0

            Assert.Equal(new[] { 0, 2, 0 }, fired);
            Assert.Equal(3, cycler.FocusChangeCount); // count only increments on focused items
            cycler.Stop();
        }

        [Fact]
        public async Task WrapsAroundModuloItemCount()
        {
            var fired = new List<int>();
            var gate = new StepDelay();
            var cycler = Build(2, _ => true, fired, gate);

            cycler.Start(2);        // 0
            await gate.StepAsync(); // 1
            await gate.StepAsync(); // 0
            await gate.StepAsync(); // 1

            Assert.Equal(new[] { 0, 1, 0, 1 }, fired);
            cycler.Stop();
        }

        [Fact]
        public void ExitsViaOnExhaustedWhenNothingIsFocusable()
        {
            var fired = new List<int>();
            var gate = new StepDelay();
            bool exhausted = false;
            var cycler = new FocusCycler(
                new InlineUiDispatcher(),
                i => fired.Add(i),
                () => TimeSpan.Zero,
                mayFocus: _ => false,
                delay: gate.Delay,
                onExhausted: () => exhausted = true);

            cycler.Start(3);

            Assert.True(exhausted);
            Assert.Empty(fired);
            Assert.Equal(0, cycler.FocusChangeCount);
        }

        [Fact]
        public void StopPreventsFurtherFiring()
        {
            var fired = new List<int>();
            var gate = new StepDelay();
            var cycler = Build(3, _ => true, fired, gate);

            cycler.Start(3); // fires 0
            cycler.Stop();

            Assert.Equal(new[] { 0 }, fired);
        }
    }
}
