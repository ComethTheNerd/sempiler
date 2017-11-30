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
/// <reference path="./ViewController.d.ts"/>

declare class UIApplication extends UIResponder {

    static get shared() : UIApplication

    public get applicationState() : UIApplicationState;
}

declare enum UIApplicationState {
    active,
    inactive,
    background
}

declare class UIWindow extends UIView {
    backgroundColor : UIColor
    rootViewController : opt<UIViewController>
    makeKeyAndVisible() : void;
}

declare class UIScreen {
    static const main : UIScreen;

    bounds : CGRect;
}

declare class UIColor {
    static const white : UIColor;
    static const lightGray : UIColor;
    static const red : UIColor;

}

declare abstract class UIResponder extends NSObject {

}


// https://developer.apple.com/documentation/uikit/uiview
declare class UIView extends UIResponder {
    constructor(frame : label<'frame'> | CGRect);
    constructor(coder : label<'coder'> | NSCoder) : terminator
    frame : CGRect;
    backgroundColor : UIColor;
    textColor : UIColor;
    get safeAreaInsets() : UIEdgeInsets;
    addSubview(_: UIView) : void;
    setNeedsLayout(): void;
    layoutSubviews() : void;
}

declare class UIViewController extends UIResponder {
    
    view : UIView

    constructor(nibName : label<'nibName'> | opt<string>, bundle : label<'bundle'> | opt<Bundle>);
    constructor/*?*/(coder : label<'coder'> | NSCoder);

    viewWillLayoutSubViews();
    viewDidLoad() : void;
    didReceiveMemoryWarning() : void;
    // addSubView(vi)
}

declare /*@struct*/ class UIEdgeInsets {

}

// https://developer.apple.com/documentation/uikit/uiimageview
declare class UIImageView extends UIView
{
    constructor(image : label<'image'> | opt<UIImage>)
    constructor(image : label<'image'> | opt<UIImage>, highlightedImage : label<'highlightedImage'> | opt<UIImage>)

    image : opt<UIImage>
    highlightedImage : opt<UIImage>
}

// https://developer.apple.com/documentation/uikit/uiimage
declare class UIImage
{
    constructor/*?*/(named : label<'named'> | string, _in : label<'in'> | opt<Bundle>, compatibleWith : label<'compatibleWith'> | opt<UITraitCollection>)
    constructor/*?*/(named : label<'named'> | string)
    constructor(imageLiteralResourceName : label<'imageLiteralResourceName'> | string);
}

declare class UITraitCollection {}

declare interface UIApplicationDelegate {}

declare class UIApplicationLaunchOptionsKey {};

// https://developer.apple.com/documentation/uikit/uicontrol
declare class UIControl extends UIView {

}

// https://developer.apple.com/documentation/uikit/uisegmentedcontrol
declare class UISegmentedControl extends UIControl {
    constructor(items : label<'items'> | opt<any[]>)

    selectedSegmentIndex : Int;
}

declare class UILabel extends UIView {
    constructor();
    text : opt<string>;
    textAlignment : NSTextAlignment;
    numberOfLines : Int;
    center : CGPoint;
}

enum NSTextAlignment {
    center
}