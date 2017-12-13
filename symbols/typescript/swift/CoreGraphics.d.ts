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

declare class CGRect {
    static zero : CGRect;

    public constructor(x: CGFloat, y: CGFloat, width: CGFloat, height: CGFloat);

    public constructor(x: Double, y: Double, width: Double, height: Double);

    public constructor(x: Int, y: Int, width: Int, height: Int);

    public constructor/*?*/(dictionaryRepresentation: CFDictionary);

    public divided(atDistance: CGFloat, from: CGRectEdge) : { slice: CGRect, remainder: CGRect }
}

declare class CGPoint {
    public constructor(x : label<'x'> | Double, y : label<'y'> | Double)
}

declare type CGFloat = Float;

declare enum CGRectEdge
{
    minXEdge,
    minYEdge,
    maxXEdge,
    maxYEdge
}

@struct declare class CGSize
{
    public width : CGFloat;
    public height : CGFloat;

    public static zero : CGSize;

    public constructor()
    public constructor(dictionaryRepresentation : label<'dictionaryRepresentation'> | CFDictionary) : terminator
    public constructor(from : label<'from'> | Decoder);
    public constructor(width : label<'width'> | Double, height : label<'height'> | Double)
    public constructor(width : label<'width'> | CGFloat, height : label<'height'> | CGFloat)
    public constructor(width : label<'width'> | Int, height : label<'height'> | Int)
    
    public encode(to : label<'to'> | Encoder)

    public applying(_ : CGAffineTransform) : CGSize;

    public get dictionaryRepresentation() : CGDictionary;
 
    public get customMirror() : Mirror;

    public equalTo(_ : CGSize) : Bool;
}

@struct declare class CGAffineTransform
{
    public constructor(rotationAngle : label<'rotationAngle'> | CGFloat)
    public constructor(scaleX : label<'scaleX'> | CGFloat, y : label<'y'> | CGFloat)
    public constructor(translationX : label<'translationX'> | CGFloat, y : label<'y'> | CGFloat)
    public constructor()
    public constructor(a : label<'a'> | CGFloat, b : label<'b'> | CGFloat, c : label<'c'> | CGFloat, d : label<'d'> | CGFloat, tx : label<'tx'> | CGFloat, ty : label<'ty'> | CGFloat)
    public constructor(from : label<'from'> | Decoder)

    public isIdentity : Bool;
    public a : CGFloat;
    public b : CGFloat;
    public c : CGFloat;
    public d : CGFloat;
    public tx : CGFloat;
    public ty : CGFloat;

    public static identity : CGAffineTransform;

    public concatenating(_ : CGAffineTransform) : CGAffineTransform;

    public inverted() : CGAffineTransform;

    public rotated(by : label<'by'> | CGFloat) : CGAffineTransform;
    
    public scaledBy(x : label<'x'> | CGFloat, y : label<'y'> | CGFloat) : CGAffineTransform;

    public translatedBy(x : label<'x'> | CGFloat, y : label<'y'> | CGFloat) : CGAffineTransform;

    public encode(to : label<'to'> | Encoder)
}