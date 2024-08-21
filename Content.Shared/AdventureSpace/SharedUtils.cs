namespace Content.Shared.AdventureSpace;

public static class SharedUtils
{
    public static void Repeat(int count, Action action)
    {
        var counter = 0;
        while (counter != count)
        {
            action.Invoke();
            counter++;
        }
    }
}
