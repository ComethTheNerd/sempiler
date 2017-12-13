/** 
BSD License

For Sempiler software

Copyright (c) 2017-present, Quantum Commune Ltd. All rights reserved.

Redistribution and use in source and binary forms, with or without modification,
are permitted provided that the following conditions are met:

 * Redistributions of source code must retain the above copyright notice, this
   list of conditions and the following disclaimer.

 * Redistributions in binary form must reproduce the above copyright notice,
   this list of conditions and the following disclaimer in the documentation
   and/or other materials provided with the distribution.

 * Neither the name Quantum Commune nor the names of its contributors may be used to
   endorse or promote products derived from this software without specific
   prior written permission.

THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND
ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT HOLDER OR CONTRIBUTORS BE LIABLE FOR
ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES
(INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON
ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
(INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
*/

/// <reference no-default-lib="true"/>

declare interface UIView {
    configureLayout(block : (layout : YGLayout) => void)
    yoga : {
        applyLayout(preservingOrigin : boolean) : void
    }
}

declare interface YGLayout
{
    isIncludedInLayout : Bool;
    isEnabled : Bool;

    direction : YGDirection;
    flexDirection : YGFlexDirection;
    justifyContent : YGJustify;
    alignContent : YGAlign;
    alignItems : YGAlign;
    alignSelf : YGAlign;
    position : YGPositionType;
    flexWrap : YGWrap;
    overflow : YGOverflow;
    display : YGDisplay;

    flexGrow : CGFloat;
    flexShrink : CGFloat;
    flexBasis : YGValue;

    left : YGValue;
    top : YGValue;
    right : YGValue;
    bottom : YGValue;
    start : YGValue;
    end : YGValue;

    marginLeft : YGValue;
    marginTop : YGValue;
    marginRight : YGValue;
    marginBottom : YGValue;
    marginStart : YGValue;
    marginEnd : YGValue;
    marginHorizontal : YGValue;
    marginVertical : YGValue;
    margin : YGValue;

    paddingLeft : YGValue;
    paddingTop : YGValue;
    paddingRight : YGValue;
    paddingBottom : YGValue;
    paddingStart : YGValue;
    paddingEnd : YGValue;
    paddingHorizontal : YGValue;
    paddingVertical : YGValue;
    padding : YGValue;

    borderLeftWidth : CGFloat;
    borderTopWidth : CGFloat;
    borderRightWidth : CGFloat;
    borderBottomWidth : CGFloat;
    borderStartWidth : CGFloat;
    borderEndWidth : CGFloat;
    borderWidth : CGFloat;

    width : YGValue;
    height : YGValue;
    minWidth : YGValue;
    minHeight : YGValue;
    maxWidth : YGValue;
    maxHeight : YGValue;

    aspectRatio : CGFloat;

    resolvedDirection  : YGDirection;

    applyLayout(preservingOriging : label<'preservingOrigin'> | Bool) : Void
    applyLayout(preservingOriging : label<'preservingOrigin'> | Bool, dimensionFlexibility : label<'dimensionFlexibility'> | YGDimensionFlexibility) : Void

    intrinsicSize : CGSize;

    calculateLayout(_with : label<'width'> | CGSize) : CGSize

    numberOfChildren : NSUInteger;
    
    isLeaf : Bool;
    isDirty : Bool;


    markDirty() : void;

}

@struct declare class YGValue 
{
    public value : number;
    public unit : YGUnit;
}

declare enum YGUnit
{
    undefined,
    point,
    percent,
    auto
}

declare enum YGAlign
{
    auto,
    flexStart,
    center,
    flexEnd,
    stretch,
    baseline,
    spaceBetween,
    spaceAround
}

declare enum YGDirection
{
    inherit,
    ltr,
    rtl
}

declare enum YGJustify
{
    flexStart,
    center,
    flexEnd,
    spaceBetween,
    spaceAround,
    spaceEvenly
}