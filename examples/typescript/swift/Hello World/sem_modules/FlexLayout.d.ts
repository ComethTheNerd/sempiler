declare interface UIView 
{
    flex : Flex;
}

declare class Flex {
    addItem() : Flex;
    addItem(view : UIView) : Flex;
    aspectRatio(value: CGFloat) : Flex;
    aspectRatio(imageView : label<'of'> | UIImageView)
    backgroundColor(color : UIColor) : Flex;
    define(closure : (flex : Flex) => void) : Flex;
    direction(value : Direction) : Flex;
    grow(value : CGFloat) : Flex;
    height(value : CGFloat) : Flex;
    layout(mode : label<'mode'> | Flex.LayoutMode) : Flex;
    marginBottom(value : CGFloat) : Flex;
    marginTop(value : CGFloat) : Flex;
    padding(value : CGFloat) : Flex;
    paddingLeft(value : CGFloat) : Flex;
    shrink(value : CGFloat) : Flex;
    width(value : CGFloat) : Flex;
    justifyContent(value : Flex.JustifyContent) : Flex;
    alignItems(value : Flex.AlignItems) : Flex;
    alignSelf(value : Flex.AlignSelf) : Flex;
}

declare module Flex 
{
    enum Direction {
        column,
        row
    }

    enum LayoutMode {
        fitContainer,
        adjustHeight,
        adjustWidth
    }

    enum JustifyContent {
        start,
        center,
        end,
        spaceBetween,
        spaceAround
    }
    
    enum AlignContent {
        stretch,
        start,
        center,
        end,
        spaceBetween,
        spaceAround
    }
    
    enum AlignItems {
        stretch,
        start,
        center,
        end,
        baseline
    }
    
    enum AlignSelf {
        auto,
        stretch,
        start,
        center,
        end,
        baseline
    }
}