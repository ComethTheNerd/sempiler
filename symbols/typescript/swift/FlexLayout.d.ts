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