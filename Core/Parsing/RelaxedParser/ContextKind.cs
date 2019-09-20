using System;
using System.IO;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;

namespace Sempiler.Parsing
{
    [Flags]
    public enum ContextKind
    {
        SourceElements = 1 << 0, // Elements in source file
        BlockStatements= 1 << 1, // Statements in block
        SwitchClauses = 1 << 2, // Clauses in switch statement
        SwitchClauseStatements = 1 << 3, // Statements in switch clause
        TypeMembers = 1 << 4, // Members in interface or type literal
        ClassMembers = 1 << 5, // Members in class declaration
        EnumMembers = 1 << 6, // Members in enum declaration
        HeritageClauseElement = 1 << 7, // Elements in a heritage clause
        DataValueDeclarations = 1 << 8, // Variable declarations in variable statement
        ObjectBindingElements = 1 << 9, // Binding elements in object binding list
        ArrayBindingElements = 1 << 10, // Binding elements in array binding list
        ArgumentExpressions = 1 << 11, // Expressions in argument list
        ObjectLiteralMembers= 1 << 12, // Members in object literal
        JsxAttributes = 1 << 13, // Attributes in jsx element
        JsxChildren = 1 << 14, // Things between opening and closing JSX tags
        ArrayLiteralMembers = 1 << 15, // Members in array literal
        Parameters = 1 << 16, // Parameters in parameter list
        RestProperties = 1 << 17, // Property names in a rest type list
        TypeParameters = 1 << 18, // Type parameters in type parameter list
        TypeArguments = 1 << 19, // Type arguments in type argument list
        TupleElementTypes = 1 << 20, // Element types in tuple element type list
        HeritageClauses = 1 << 21, // Heritage clauses for a class or interface declaration.
        ImportOrExportSpecifiers = 1 << 22, // Named import clause's import specifier list
        JSDocFunctionParameters = 1 << 23,
        JSDocTypeArguments = 1 << 24,
        JSDocRecordMembers = 1 << 25,
        JSDocTupleTypes = 1 << 26
    }
}