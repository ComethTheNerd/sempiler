/// <reference path="./ViewController.d.ts"/>

declare class UIApplication {}

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