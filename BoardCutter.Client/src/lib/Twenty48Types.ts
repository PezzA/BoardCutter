export interface CellType {
    Point: Point;
    Value: number;
    Id: string;
    Status: number;
    New: boolean;
    Destroy: boolean;
    Merged: boolean;
}

export interface Point {
    X: number;
    Y: number;
}