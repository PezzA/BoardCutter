using BoardCutter.Core;

namespace BoardCutter.Games.Twenty48.Tests;

[Trait("Category", "UnitTests")]
public class ShuntTests
{
    public static IEnumerable<object[]> GetShuntCellUpData()
    {
        return
        [
            // @formatter:off
            [
                new Dictionary<int, NumberCell>
                { 
                    {1, new NumberCell(1, 2, new Point2D(0, 3), false, false)}
                },
                4,
                new Dictionary<int, NumberCell>
                { 
                    {1, new NumberCell(1, 2, new Point2D(0, 0), false, false)}
                },
                true,
                0
            ],
            [
                new Dictionary<int, NumberCell>
                { 
                    {1, new NumberCell(1, 2, new Point2D(0, 3), false, false)},
                    {2, new NumberCell(2, 2, new Point2D(0, 2), false, false)}
                },
                4,
                new Dictionary<int, NumberCell>
                { 
                    {1, new NumberCell(1, 2, new Point2D(0, 0), false, true)},
                    {2, new NumberCell(2, 2, new Point2D(0, 0), false, true)},
                    {3, new NumberCell(3, 4, new Point2D(0, 0), true, false)}
                },
                true,
                4
            ]
            // @formatter:on
        ];
    }

    [Theory]
    [MemberData(nameof(GetShuntCellUpData))]
    public void CellShuntUp_Succeed(
        Dictionary<int, NumberCell> inputGrid,
        int gridSize,
        Dictionary<int, NumberCell> expectedGrid,
        bool expectedHasChanged,
        int expectedScoreIncrement)
    {
        (bool actualHasChanged, int actualScoreIncrement) = GameActor.ShuntUp(
            inputGrid,
            gridSize);

        Assert.Equivalent(expectedGrid, inputGrid);
        Assert.Equal(expectedHasChanged, actualHasChanged);
        Assert.Equal(expectedScoreIncrement, actualScoreIncrement);
    }

    public static IEnumerable<object[]> GetShuntCellDownData()
    {
        return
        [
            // @formatter:off
            [
                new Dictionary<int, NumberCell>
                { 
                    {1, new NumberCell(1, 2, new Point2D(0, 0), false, false)}
                },
                4,
                new Dictionary<int, NumberCell>
                { 
                    {1, new NumberCell(1, 2, new Point2D(0, 3), false, false)}
                },
                true,
                0
            ],
            [
                new Dictionary<int, NumberCell>
                { 
                    {1, new NumberCell(1, 2, new Point2D(0, 1), false, false)},
                    {2, new NumberCell(2, 2, new Point2D(0, 2), false, false)}
                },
                4,
                new Dictionary<int, NumberCell>
                { 
                    {1, new NumberCell(1, 2, new Point2D(0, 3), false, true)},
                    {2, new NumberCell(2, 2,new Point2D( 0, 3), false, true)},
                    {3, new NumberCell(3, 4,new Point2D( 0, 3), true, false)}
                },
                true,
                4
            ]
            // @formatter:on
        ];
    }

    [Theory]
    [MemberData(nameof(GetShuntCellDownData))]
    public void CellShuntDown_Succeed(
        Dictionary<int, NumberCell> inputGrid,
        int gridSize,
        Dictionary<int, NumberCell> expectedGrid,
        bool expectedHasChanged,
        int expectedScoreIncrement)
    {
        (bool actualHasChanged, int actualScoreIncrement) = GameActor.ShuntDown(
            inputGrid,
            gridSize);

        Assert.Equivalent(expectedGrid, inputGrid);
        Assert.Equal(expectedHasChanged, actualHasChanged);
        Assert.Equal(expectedScoreIncrement, actualScoreIncrement);
    }

    public static IEnumerable<object[]> GetShuntCellLeftData()
    {
        return
        [
            // @formatter:off
            [
                new Dictionary<int, NumberCell>
                { 
                    {1, new NumberCell(1, 2, new Point2D(3, 0), false, false)}
                },
                4,
                new Dictionary<int, NumberCell>
                { 
                    {1, new NumberCell(1, 2, new Point2D(0, 0), false, false)}
                },
                true,
                0
            ],
            [
                new Dictionary<int, NumberCell>
                { 
                    {1, new NumberCell(1, 2, new Point2D(3, 0), false, false)},
                    {2, new NumberCell(2, 2, new Point2D(2, 0), false, false)}
                },
                4,
                new Dictionary<int, NumberCell>
                { 
                    {1, new NumberCell(1, 2, new Point2D(0, 0), false, true)},
                    {2, new NumberCell(2, 2, new Point2D(0, 0), false, true)},
                    {3, new NumberCell(3, 4, new Point2D(0, 0), true, false)}
                },
                true,
                4
            ]
            // @formatter:on
        ];
    }

    [Theory]
    [MemberData(nameof(GetShuntCellLeftData))]
    public void CellShuntLeft_Succeed(
        Dictionary<int, NumberCell> inputGrid,
        int gridSize,
        Dictionary<int, NumberCell> expectedGrid,
        bool expectedHasChanged,
        int expectedScoreIncrement)
    {
        (bool actualHasChanged, int actualScoreIncrement) = GameActor.ShuntLeft(
            inputGrid,
            gridSize);

        Assert.Equivalent(expectedGrid, inputGrid);
        Assert.Equal(expectedHasChanged, actualHasChanged);
        Assert.Equal(expectedScoreIncrement, actualScoreIncrement);
    }

    public static IEnumerable<object[]> GetShuntCellRightData()
    {
        return
        [
            // @formatter:off
            [
                new Dictionary<int, NumberCell>
                { 
                    {1, new NumberCell(1, 2, new Point2D(0, 0), false, false)}
                },
                4,
                new Dictionary<int, NumberCell>
                { 
                    {1, new NumberCell(1, 2, new Point2D(3, 0), false, false)}
                },
                true,
                0
            ],
            [
                new Dictionary<int, NumberCell>
                { 
                    {1, new NumberCell(1, 2, new Point2D(0, 0), false, false)},
                    {2, new NumberCell(2, 2, new Point2D(1, 0), false, false)}
                },
                4,
                new Dictionary<int, NumberCell>
                { 
                    {1, new NumberCell(1, 2, new Point2D(3, 0), false, true)},
                    {2, new NumberCell(2, 2, new Point2D(3, 0), false, true)},
                    {3, new NumberCell(3, 4, new Point2D(3, 0), true, false)}
                },
                true,
                4
            ]
            // @formatter:on
        ];
    }

    [Theory]
    [MemberData(nameof(GetShuntCellRightData))]
    public void CellShuntRight_Succeed(
        Dictionary<int, NumberCell> inputGrid,
        int gridSize,
        Dictionary<int, NumberCell> expectedGrid,
        bool expectedHasChanged,
        int expectedScoreIncrement)
    {
        (bool actualHasChanged, int actualScoreIncrement) = GameActor.ShuntRight(
            inputGrid,
            gridSize);

        Assert.Equivalent(expectedGrid, inputGrid);
        Assert.Equal(expectedHasChanged, actualHasChanged);
        Assert.Equal(expectedScoreIncrement, actualScoreIncrement);
    }

    public static IEnumerable<object[]> GetShuntCellData()
    {
        return
        [
            // @formatter:off
            [
                1,
                new Dictionary<int, NumberCell>
                { 
                    {1, new NumberCell(1, 2, new Point2D(0, 0), false, false)}
                },
                1,
                0,
                4,
                new Dictionary<int, NumberCell>
                { 
                    {1, new NumberCell(1, 2, new Point2D(3, 0), false, false)}
                },
                true,
                0
            ],
            [
                1,
                new Dictionary<int, NumberCell>
                { 
                    {1, new NumberCell(1, 2, new Point2D(0, 0), false, false)},
                    {2, new NumberCell(2, 2, new Point2D(3, 0), false, false)}
                },
                1,
                0,
                4,
                new Dictionary<int, NumberCell>
                { 
                    {1, new NumberCell(1, 2, new Point2D(3, 0), false, true)},
                    {2, new NumberCell(2, 2, new Point2D(3, 0), false, true)},
                    {3, new NumberCell(3, 4, new Point2D(3, 0), true, false)}
                },
                true,
                4
            ]
            // @formatter:on
        ];
    }

    [Theory]
    [MemberData(nameof(GetShuntCellData))]
    public void CellShunt_Succeeds(
        int cellId,
        Dictionary<int, NumberCell> inputGrid,
        int xInc,
        int yInc,
        int gridSize,
        Dictionary<int, NumberCell> expectedGrid,
        bool expectedHasChanged,
        int expectedScoreIncrement)
    {
        (bool actualHasChanged, int actualScoreIncrement) = GameActor.ShuntCell(
            cellId,
            inputGrid,
            xInc,
            yInc,
            gridSize);

        Assert.Equivalent(expectedGrid, inputGrid);
        Assert.Equal(expectedHasChanged, actualHasChanged);
        Assert.Equal(expectedScoreIncrement, actualScoreIncrement);
    }
}