declare interface UIView 
{
    pin : Pin;
}

declare class Pin {
    top() : Pin;
    left() : Pin;
    right() : Pin;
    horizontally() : Pin;
    margin(value : CGFloat) : Pin;
}