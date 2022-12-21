namespace AppBroker.Zigbee2Mqtt.Math
{
    public class SteppedFunc
    {
        private readonly (int since, float factor)[] steps;

        public SteppedFunc(params (int, float)[] steps)
            => this.steps = steps.OrderBy(x => x.Item1).ToArray();

        /// <summary>
        /// Evaluates at a given position.
        /// </summary>
        /// <param name="px">The position to evaluate at.</param>
        /// <returns>The evaluated value.</returns>
        public int Evaluate(int px)
        {
            if (steps.Length == 0)
                return 0;


            var val = 0f;
            var stepsDone = 0;

            for (int i = 0; i < steps.Length; i++)
            {
                var step = steps[i];

                var toDo = step.since - stepsDone;
                var completed = toDo + stepsDone >= px;
                if (completed)
                    toDo = px - stepsDone;

                val += toDo * step.factor;
                stepsDone += toDo;
                if (completed)
                    return (int)System.Math.Round(val);
            }

            return (int)System.Math.Round(val);

        }
    }
}
