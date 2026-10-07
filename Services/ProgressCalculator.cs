using DexGameBacklog.Api.Contracts;

namespace DexGameBacklog.Api.Services;

public static class ProgressCalculator
{
    public static ProgressResponse Calculate(int completed, int total)
    {
        var percentage = total == 0
            ? null
            : (int?)Math.Round(
                (double)completed / total * 100,
                MidpointRounding.AwayFromZero);

        return new ProgressResponse(completed, total, percentage);
    }
}
