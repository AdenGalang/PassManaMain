namespace PassManaAlpha.Core
{
    public static class DecryptTextHelper
    {
        private static readonly char[] GlitchChars = { '#', '&', '^', '%', '*', '@', '$', '!', '?' };
        private static readonly Random Rand = new();

        public static async Task AnimateTextComplexAsync(
            string targetMessage,
            Action<int, char, bool> updateCharAction,
            CancellationToken cancellationToken)
        {
            int totalLength = targetMessage.Length;

            for (int i = 0; i < totalLength; i++)
            {
                char randomChar = char.IsWhiteSpace(targetMessage[i]) ? ' ' : GlitchChars[Rand.Next(GlitchChars.Length)];
                updateCharAction(i, randomChar, false);
            }
            await Task.Delay(90, cancellationToken); // initial display duration

            for (int revealIndex = 0; revealIndex < totalLength; revealIndex++)
            {
                if (char.IsWhiteSpace(targetMessage[revealIndex]))
                {
                    updateCharAction(revealIndex, ' ', true);
                    continue;
                }
                // cyrves
                double progress = (double)revealIndex / totalLength;
                double curveFactor = 0.5 + Math.Pow(progress, 2) * 1.5;
                int adjustedGlitchFrameCount = (int)(2 * curveFactor);
                int adjustedFlickerSpeed = (int)(12 * curveFactor);
                int adjustedStepDelay = (int)(16 * curveFactor);
                // curves
                for (int glitchFrame = 0; glitchFrame < adjustedGlitchFrameCount; glitchFrame++)
                {
                    if (cancellationToken.IsCancellationRequested) return;

                    for (int j = revealIndex; j < totalLength; j++)
                    {
                        if (!char.IsWhiteSpace(targetMessage[j]))
                        {
                            char rawSymbol = GlitchChars[Rand.Next(GlitchChars.Length)];
                            updateCharAction(j, rawSymbol, false);
                        }
                    }

                    await Task.Delay(adjustedFlickerSpeed, cancellationToken);
                }

                updateCharAction(revealIndex, targetMessage[revealIndex], true);
                await Task.Delay(adjustedStepDelay, cancellationToken);
            }
        }
    }
}