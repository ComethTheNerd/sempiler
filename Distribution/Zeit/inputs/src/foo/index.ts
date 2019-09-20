import * from "android.widget";

// const s : string = "Did this work?";

// function helloWorld() : string
// {
//     return s + "Yo";
// }
// export function hello(){}
module.exports = () => {
    // Creating a new RelativeLayout
    const relativeLayout : RelativeLayout = new RelativeLayout(this);
    
    // Defining the RelativeLayout layout parameters.
    // In this case I want to fill its parent
    const rlp : RelativeLayout.LayoutParams = new RelativeLayout.LayoutParams(
            RelativeLayout.LayoutParams.FILL_PARENT,
            RelativeLayout.LayoutParams.FILL_PARENT);
    
    // Creating a new TextView
    const tv : TextView = new TextView(this);
    tv.setText("Hello There!!");
    
    // Defining the layout parameters of the TextView
    const lp : RelativeLayout.LayoutParams = new RelativeLayout.LayoutParams(
            RelativeLayout.LayoutParams.WRAP_CONTENT,
            RelativeLayout.LayoutParams.WRAP_CONTENT);
    
    lp.addRule(RelativeLayout.CENTER_IN_PARENT);
    
    // Setting the parameters on the TextView
    tv.setLayoutParams(lp);
    
    // Adding the TextView to the RelativeLayout as a child
    relativeLayout.addView(tv);
    
    // Setting the RelativeLayout as our content view
    setContentView(relativeLayout, rlp);
}