namespace BoardCutter.Games.CantStop;

public interface IDiceThrower
{
    public int[] Throw(int max, int dieCount);
}

public class ProgrammableDiceThrower : IDiceThrower
{
    private readonly int[][] _throws;
    private int _index = 0;

    public ProgrammableDiceThrower(int[][] throws)
    {
        _throws = throws;
    }

    public int[] Throw(int max, int dieCount)
    {
        var returnThrow = _throws[_index];
        _index++;

        return returnThrow;
    }
}

public class RandomDiceThower : IDiceThrower
{

    public int[] Throw(int max, int dieCount)
    {
        throw new NotImplementedException();
    }
}