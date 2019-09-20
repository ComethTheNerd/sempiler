export interface Range 
{
    start : number;
    end : number;
}

export namespace RangeHelpers
{
    export function Length(range : Range) : number
    {
        return range.end - range.start;
    }

    export function Default() : Range
    {
        return { start : -1, end : - 1 }
    }

    export function IsValid({ start, end } : Range) : boolean
    {
        return start > -1 && end >= start;
    }
}