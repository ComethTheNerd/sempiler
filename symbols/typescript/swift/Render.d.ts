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

declare interface NodeType 
{
    renderedView : opt<UIView>;
    // key : Key;
    children : NodeType[]
    add(children : label<'children'> | NodeType[]) : NodeType;
    add(child : label<'child'> | NodeType) : NodeType;
    index : Int;
    debugType : String;

}

declare class Node<V extends UIView> implements NodeType
{
    /* NodeType */
    renderedView : opt<UIView>;
    // key : Key;
    children : NodeType[]
    add(children : label<'children'> | NodeType[]) : NodeType;
    add(child : label<'child'> | NodeType) : NodeType;

    index : Int;
    debugType : String;
    /**/

    public constructor(
        reuseIdentifier? : label<'reuseIdentifier'> | String,
        key : label<'key'> | String?,
        resetBeforeReuse? : label<'resetBeforeReuse'> | Bool,
        children? : label<'children'> | NodeType[],
        create? : label<'create'> | escaping | (() => V),
        props? :label<'props'> | escaping | ((v : V, l : YGLayout, s : CGSize) => Void)
    )

    // [dho] this is just to allow us to call the constructor in a form
    // that only has the props block.. because currently Sempiler cannot handle
    // the case when we don't supply the parameters before the one we want - 12/12/17
    public constructor(props : label<'props'> | escaping | ((v : V, l : YGLayout, s : CGSize) => Void))
}

declare interface StateType extends ReflectedStringConvertible
{
    // new ()
}

@struct declare class NilState implements StateType 
{
    public description : String
}

declare class ComponentView<S extends StateType> extends UIView implements ComponentViewType
{
    public defaultOptions : RenderOption[]

    public state : S;

    public anyState : Render.StateType;

    public isStateless : Bool;

    public setState(options : label<'options'> | RenderOption[], change : label<'change'> | ((_ : ref | S) => Void))
    public setState(change : label<'change'> | ((_ : ref | S) => Void))

    public childrenComponent : dict<Key, AnyComponentView>;

    public childrenComponentAutoIncrementKey: Int;
}

declare interface AnyComponentView extends NSObjectProtocol, ReflectedStringConvertible
{
    public isStateless : Bool;
    public childrenComponent : dict<Key, AnyComponentView>;
    public childrenComponentAutoIncrementKey : Int;

}

declare interface ComponentViewType extends AnyComponentView 
{
    /*
      associatedtype StateType
  /// The current state of the component.
  var state: StateType { get set }

  /// Required method.
  /// When called, it should examine the component properties and the state  and return a Node tree.
  /// This method is called every time 'render' is invoked.
  func render() -> NodeType
    */
    public render() : NodeType
}

// [dho] @HACK
// https://github.com/alexdrone/Render/blob/8b91a416e2b16006788cb1961e2ee0acf0f8c292/Render/core/ComponentController.swift
declare interface ComponentController/*<C extends ComponentViewType>*/ 
{
    // component : ComponentViewType;

    configureComponentProps();
    addComponentToViewControllerHierarchy();
    renderComponent(options? : label<'options'> | RenderOption[]);
    onLayout(duration : label<'duration'> | TimeInterval, component : label<'component'> | AnyComponentView, size : label<'size'> | CGSize);
    configureComponentProps();
}

declare enum RenderOption
{
    preventViewHierarchyDiff,
    animated,
    // case animated(duration: TimeInterval, options: UIViewAnimationOptions)
    preventUpdate,
    preventOnLayoutCallback,
    _animated,
    _done
}

declare interface UIView 
{
   onTap(handler : escaping | ((_ : UIGestureRecognizer) => Void)) : void

   cornerRadius : CGFloat;
   borderWidth : CGFloat;
   borderColor : ptr<UIColor>;
   shadowOpacity : CGFloat;
   shadowRadius : CGFloat;
   shadowOffset : CGSize;
   shadowColor : ptr<UIColor>;
}

declare interface UIButton
{
    text : ptr<NSString>;
    highlightedText : ptr<NSString>;
    selectedText : ptr<NSString>;
    disabledText : ptr<NSString>;

    textColor : ptr<UIColor>;
    highlightedTextColor : ptr<UIColor>;
    selectedTextColor : ptr<UIColor>;
    disabledTextColor : ptr<UIColor>;
    backgroundColorImage : ptr<UIColor>;

    backgroundImage : ptr<UIImage>;
    highlightedBackgroundImage : ptr<UIImage>;
    selectedBackgroundImage : ptr<UIImage>;
    disabledBackgroundImage : ptr<UIImage>;
    
    image : ptr<UIImage>;
    highlightedImage : ptr<UIImage>;
    selectedImage : ptr<UIImage>;
    disabledImage : ptr<UIImage>;
}

declare interface UIImage
{
    yg_imageWithColor(color : ptr<UIColor>) : ptr<UIImage>;
    yg_imageWithColor(color : ptr<UIColor>, size : label<'size'> | CGSize) : ptr<UIImage>;
}

// // https://github.com/alexdrone/Render/blob/80241b9c506c2fc25fae5c7e45dc328d776ad0c9/samples/Neutrino/src/scenes/fragments/Stylesheet.swift
// declare enum Typography
// {
//     extraSmallBold,
//     small,
//     smallBold,
//     medium,
//     mediumBold
// }