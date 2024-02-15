using System;

[Flags]
public enum BlockProjectedShapes
{
    None = 0,
    LeftUpperTriangle = 1 << 0,
    MiddleUpperTriangle = 1 << 1,
    RightUpperTriangle = 1 << 2,
    LeftLowerTriangle = 1 << 3,
    MiddleLowerTriangle = 1 << 4,
    RightLowerTriangle = 1 << 5,
    Walkable = LeftUpperTriangle | MiddleUpperTriangle,
    FullHexagon = LeftUpperTriangle | MiddleUpperTriangle | RightUpperTriangle | LeftLowerTriangle | MiddleLowerTriangle | RightLowerTriangle,
}
