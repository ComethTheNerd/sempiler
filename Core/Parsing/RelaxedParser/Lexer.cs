using System;
using System.IO;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Sempiler;
using Sempiler.AST;
using Sempiler.Parsing;
using Sempiler.Diagnostics;
using static Sempiler.Diagnostics.DiagnosticsHelpers;
using Sempiler.Parsing.Diagnostics;

namespace Sempiler.Parsing
{

    public static class S1
    {
        public enum LanguageVariant
        {
            Standard,
            Jsx
        }


        public static Dictionary<string, SyntaxKind> DefaultTokenMap = new Dictionary<string, SyntaxKind>
{
{ "{", SyntaxKind.OpenBraceToken },
{ "}", SyntaxKind.CloseBraceToken },
{ "(", SyntaxKind.OpenParenToken },
{ ")", SyntaxKind.CloseParenToken },
{ "[", SyntaxKind.OpenBracketToken },
{ "]", SyntaxKind.CloseBracketToken },
{ ".", SyntaxKind.DotToken },
{ "...", SyntaxKind.DotDotDotToken },
{ ";", SyntaxKind.SemicolonToken },
{ ",", SyntaxKind.CommaToken },
{ "<", SyntaxKind.LessThanToken },
{ ">", SyntaxKind.GreaterThanToken },
{ "<=", SyntaxKind.LessThanEqualsToken },
{ ">=", SyntaxKind.GreaterThanEqualsToken },
{ "==", SyntaxKind.EqualsEqualsToken },
{ "!=", SyntaxKind.ExclamationEqualsToken },
{ "===", SyntaxKind.EqualsEqualsEqualsToken },
{ "!==", SyntaxKind.ExclamationEqualsEqualsToken },
{ "=>", SyntaxKind.EqualsGreaterThanToken },
{ "+", SyntaxKind.PlusToken },
{ "-", SyntaxKind.MinusToken },
{ "**", SyntaxKind.AsteriskAsteriskToken },
{ "*", SyntaxKind.AsteriskToken },
{ "/", SyntaxKind.SlashToken },
{ "%", SyntaxKind.PercentToken },
{ "++", SyntaxKind.PlusPlusToken },
{ "--", SyntaxKind.MinusMinusToken },
{ "<<", SyntaxKind.LessThanLessThanToken },
{ "</", SyntaxKind.LessThanSlashToken },
{ ">>", SyntaxKind.GreaterThanGreaterThanToken },
{ ">>>", SyntaxKind.GreaterThanGreaterThanGreaterThanToken },
{ "&", SyntaxKind.AmpersandToken },
{ "|", SyntaxKind.BarToken },
{ "^", SyntaxKind.CaretToken },
{ "!", SyntaxKind.ExclamationToken },
{ "~", SyntaxKind.TildeToken },
{ "&&", SyntaxKind.AmpersandAmpersandToken },
{ "||", SyntaxKind.BarBarToken },
{ "?", SyntaxKind.QuestionToken },
{ ":", SyntaxKind.ColonToken },
{ "=", SyntaxKind.EqualsToken },
{ "+=", SyntaxKind.PlusEqualsToken },
{ "-=", SyntaxKind.MinusEqualsToken },
{ "*=", SyntaxKind.AsteriskEqualsToken },
{ "**=", SyntaxKind.AsteriskAsteriskEqualsToken },
{ "/=", SyntaxKind.SlashEqualsToken },
{ "%=", SyntaxKind.PercentEqualsToken },
{ "<<=", SyntaxKind.LessThanLessThanEqualsToken },
{ ">>=", SyntaxKind.GreaterThanGreaterThanEqualsToken },
{ ">>>=", SyntaxKind.GreaterThanGreaterThanGreaterThanEqualsToken },
{ "&=", SyntaxKind.AmpersandEqualsToken },
{ "|=", SyntaxKind.BarEqualsToken },
{ "^=", SyntaxKind.CaretEqualsToken },
{ "@", SyntaxKind.AtToken },
};
                // public int[] UnicodeEs3IdentifierStart = { 170, 170, 181, 181, 186, 186, 192, 214, 216, 246, 248, 543, 546, 563, 592, 685, 688, 696, 699, 705, 720, 721, 736, 740, 750, 750, 890, 890, 902, 902, 904, 906, 908, 908, 910, 929, 931, 974, 976, 983, 986, 1011, 1024, 1153, 1164, 1220, 1223, 1224, 1227, 1228, 1232, 1269, 1272, 1273, 1329, 1366, 1369, 1369, 1377, 1415, 1488, 1514, 1520, 1522, 1569, 1594, 1600, 1610, 1649, 1747, 1749, 1749, 1765, 1766, 1786, 1788, 1808, 1808, 1810, 1836, 1920, 1957, 2309, 2361, 2365, 2365, 2384, 2384, 2392, 2401, 2437, 2444, 2447, 2448, 2451, 2472, 2474, 2480, 2482, 2482, 2486, 2489, 2524, 2525, 2527, 2529, 2544, 2545, 2565, 2570, 2575, 2576, 2579, 2600, 2602, 2608, 2610, 2611, 2613, 2614, 2616, 2617, 2649, 2652, 2654, 2654, 2674, 2676, 2693, 2699, 2701, 2701, 2703, 2705, 2707, 2728, 2730, 2736, 2738, 2739, 2741, 2745, 2749, 2749, 2768, 2768, 2784, 2784, 2821, 2828, 2831, 2832, 2835, 2856, 2858, 2864, 2866, 2867, 2870, 2873, 2877, 2877, 2908, 2909, 2911, 2913, 2949, 2954, 2958, 2960, 2962, 2965, 2969, 2970, 2972, 2972, 2974, 2975, 2979, 2980, 2984, 2986, 2990, 2997, 2999, 3001, 3077, 3084, 3086, 3088, 3090, 3112, 3114, 3123, 3125, 3129, 3168, 3169, 3205, 3212, 3214, 3216, 3218, 3240, 3242, 3251, 3253, 3257, 3294, 3294, 3296, 3297, 3333, 3340, 3342, 3344, 3346, 3368, 3370, 3385, 3424, 3425, 3461, 3478, 3482, 3505, 3507, 3515, 3517, 3517, 3520, 3526, 3585, 3632, 3634, 3635, 3648, 3654, 3713, 3714, 3716, 3716, 3719, 3720, 3722, 3722, 3725, 3725, 3732, 3735, 3737, 3743, 3745, 3747, 3749, 3749, 3751, 3751, 3754, 3755, 3757, 3760, 3762, 3763, 3773, 3773, 3776, 3780, 3782, 3782, 3804, 3805, 3840, 3840, 3904, 3911, 3913, 3946, 3976, 3979, 4096, 4129, 4131, 4135, 4137, 4138, 4176, 4181, 4256, 4293, 4304, 4342, 4352, 4441, 4447, 4514, 4520, 4601, 4608, 4614, 4616, 4678, 4680, 4680, 4682, 4685, 4688, 4694, 4696, 4696, 4698, 4701, 4704, 4742, 4744, 4744, 4746, 4749, 4752, 4782, 4784, 4784, 4786, 4789, 4792, 4798, 4800, 4800, 4802, 4805, 4808, 4814, 4816, 4822, 4824, 4846, 4848, 4878, 4880, 4880, 4882, 4885, 4888, 4894, 4896, 4934, 4936, 4954, 5024, 5108, 5121, 5740, 5743, 5750, 5761, 5786, 5792, 5866, 6016, 6067, 6176, 6263, 6272, 6312, 7680, 7835, 7840, 7929, 7936, 7957, 7960, 7965, 7968, 8005, 8008, 8013, 8016, 8023, 8025, 8025, 8027, 8027, 8029, 8029, 8031, 8061, 8064, 8116, 8118, 8124, 8126, 8126, 8130, 8132, 8134, 8140, 8144, 8147, 8150, 8155, 8160, 8172, 8178, 8180, 8182, 8188, 8319, 8319, 8450, 8450, 8455, 8455, 8458, 8467, 8469, 8469, 8473, 8477, 8484, 8484, 8486, 8486, 8488, 8488, 8490, 8493, 8495, 8497, 8499, 8505, 8544, 8579, 12293, 12295, 12321, 12329, 12337, 12341, 12344, 12346, 12353, 12436, 12445, 12446, 12449, 12538, 12540, 12542, 12549, 12588, 12593, 12686, 12704, 12727, 13312, 19893, 19968, 40869, 40960, 42124, 44032, 55203, 63744, 64045, 64256, 64262, 64275, 64279, 64285, 64285, 64287, 64296, 64298, 64310, 64312, 64316, 64318, 64318, 64320, 64321, 64323, 64324, 64326, 64433, 64467, 64829, 64848, 64911, 64914, 64967, 65008, 65019, 65136, 65138, 65140, 65140, 65142, 65276, 65313, 65338, 65345, 65370, 65382, 65470, 65474, 65479, 65482, 65487, 65490, 65495, 65498, 65500, };
        // public int[] UnicodeEs3IdentifierPart = { 170, 170, 181, 181, 186, 186, 192, 214, 216, 246, 248, 543, 546, 563, 592, 685, 688, 696, 699, 705, 720, 721, 736, 740, 750, 750, 768, 846, 864, 866, 890, 890, 902, 902, 904, 906, 908, 908, 910, 929, 931, 974, 976, 983, 986, 1011, 1024, 1153, 1155, 1158, 1164, 1220, 1223, 1224, 1227, 1228, 1232, 1269, 1272, 1273, 1329, 1366, 1369, 1369, 1377, 1415, 1425, 1441, 1443, 1465, 1467, 1469, 1471, 1471, 1473, 1474, 1476, 1476, 1488, 1514, 1520, 1522, 1569, 1594, 1600, 1621, 1632, 1641, 1648, 1747, 1749, 1756, 1759, 1768, 1770, 1773, 1776, 1788, 1808, 1836, 1840, 1866, 1920, 1968, 2305, 2307, 2309, 2361, 2364, 2381, 2384, 2388, 2392, 2403, 2406, 2415, 2433, 2435, 2437, 2444, 2447, 2448, 2451, 2472, 2474, 2480, 2482, 2482, 2486, 2489, 2492, 2492, 2494, 2500, 2503, 2504, 2507, 2509, 2519, 2519, 2524, 2525, 2527, 2531, 2534, 2545, 2562, 2562, 2565, 2570, 2575, 2576, 2579, 2600, 2602, 2608, 2610, 2611, 2613, 2614, 2616, 2617, 2620, 2620, 2622, 2626, 2631, 2632, 2635, 2637, 2649, 2652, 2654, 2654, 2662, 2676, 2689, 2691, 2693, 2699, 2701, 2701, 2703, 2705, 2707, 2728, 2730, 2736, 2738, 2739, 2741, 2745, 2748, 2757, 2759, 2761, 2763, 2765, 2768, 2768, 2784, 2784, 2790, 2799, 2817, 2819, 2821, 2828, 2831, 2832, 2835, 2856, 2858, 2864, 2866, 2867, 2870, 2873, 2876, 2883, 2887, 2888, 2891, 2893, 2902, 2903, 2908, 2909, 2911, 2913, 2918, 2927, 2946, 2947, 2949, 2954, 2958, 2960, 2962, 2965, 2969, 2970, 2972, 2972, 2974, 2975, 2979, 2980, 2984, 2986, 2990, 2997, 2999, 3001, 3006, 3010, 3014, 3016, 3018, 3021, 3031, 3031, 3047, 3055, 3073, 3075, 3077, 3084, 3086, 3088, 3090, 3112, 3114, 3123, 3125, 3129, 3134, 3140, 3142, 3144, 3146, 3149, 3157, 3158, 3168, 3169, 3174, 3183, 3202, 3203, 3205, 3212, 3214, 3216, 3218, 3240, 3242, 3251, 3253, 3257, 3262, 3268, 3270, 3272, 3274, 3277, 3285, 3286, 3294, 3294, 3296, 3297, 3302, 3311, 3330, 3331, 3333, 3340, 3342, 3344, 3346, 3368, 3370, 3385, 3390, 3395, 3398, 3400, 3402, 3405, 3415, 3415, 3424, 3425, 3430, 3439, 3458, 3459, 3461, 3478, 3482, 3505, 3507, 3515, 3517, 3517, 3520, 3526, 3530, 3530, 3535, 3540, 3542, 3542, 3544, 3551, 3570, 3571, 3585, 3642, 3648, 3662, 3664, 3673, 3713, 3714, 3716, 3716, 3719, 3720, 3722, 3722, 3725, 3725, 3732, 3735, 3737, 3743, 3745, 3747, 3749, 3749, 3751, 3751, 3754, 3755, 3757, 3769, 3771, 3773, 3776, 3780, 3782, 3782, 3784, 3789, 3792, 3801, 3804, 3805, 3840, 3840, 3864, 3865, 3872, 3881, 3893, 3893, 3895, 3895, 3897, 3897, 3902, 3911, 3913, 3946, 3953, 3972, 3974, 3979, 3984, 3991, 3993, 4028, 4038, 4038, 4096, 4129, 4131, 4135, 4137, 4138, 4140, 4146, 4150, 4153, 4160, 4169, 4176, 4185, 4256, 4293, 4304, 4342, 4352, 4441, 4447, 4514, 4520, 4601, 4608, 4614, 4616, 4678, 4680, 4680, 4682, 4685, 4688, 4694, 4696, 4696, 4698, 4701, 4704, 4742, 4744, 4744, 4746, 4749, 4752, 4782, 4784, 4784, 4786, 4789, 4792, 4798, 4800, 4800, 4802, 4805, 4808, 4814, 4816, 4822, 4824, 4846, 4848, 4878, 4880, 4880, 4882, 4885, 4888, 4894, 4896, 4934, 4936, 4954, 4969, 4977, 5024, 5108, 5121, 5740, 5743, 5750, 5761, 5786, 5792, 5866, 6016, 6099, 6112, 6121, 6160, 6169, 6176, 6263, 6272, 6313, 7680, 7835, 7840, 7929, 7936, 7957, 7960, 7965, 7968, 8005, 8008, 8013, 8016, 8023, 8025, 8025, 8027, 8027, 8029, 8029, 8031, 8061, 8064, 8116, 8118, 8124, 8126, 8126, 8130, 8132, 8134, 8140, 8144, 8147, 8150, 8155, 8160, 8172, 8178, 8180, 8182, 8188, 8255, 8256, 8319, 8319, 8400, 8412, 8417, 8417, 8450, 8450, 8455, 8455, 8458, 8467, 8469, 8469, 8473, 8477, 8484, 8484, 8486, 8486, 8488, 8488, 8490, 8493, 8495, 8497, 8499, 8505, 8544, 8579, 12293, 12295, 12321, 12335, 12337, 12341, 12344, 12346, 12353, 12436, 12441, 12442, 12445, 12446, 12449, 12542, 12549, 12588, 12593, 12686, 12704, 12727, 13312, 19893, 19968, 40869, 40960, 42124, 44032, 55203, 63744, 64045, 64256, 64262, 64275, 64279, 64285, 64296, 64298, 64310, 64312, 64316, 64318, 64318, 64320, 64321, 64323, 64324, 64326, 64433, 64467, 64829, 64848, 64911, 64914, 64967, 65008, 65019, 65056, 65059, 65075, 65076, 65101, 65103, 65136, 65138, 65140, 65140, 65142, 65276, 65296, 65305, 65313, 65338, 65343, 65343, 65345, 65370, 65381, 65470, 65474, 65479, 65482, 65487, 65490, 65495, 65498, 65500, };
        public static int[] UnicodeEs5IdentifierStart = { 170, 170, 181, 181, 186, 186, 192, 214, 216, 246, 248, 705, 710, 721, 736, 740, 748, 748, 750, 750, 880, 884, 886, 887, 890, 893, 902, 902, 904, 906, 908, 908, 910, 929, 931, 1013, 1015, 1153, 1162, 1319, 1329, 1366, 1369, 1369, 1377, 1415, 1488, 1514, 1520, 1522, 1568, 1610, 1646, 1647, 1649, 1747, 1749, 1749, 1765, 1766, 1774, 1775, 1786, 1788, 1791, 1791, 1808, 1808, 1810, 1839, 1869, 1957, 1969, 1969, 1994, 2026, 2036, 2037, 2042, 2042, 2048, 2069, 2074, 2074, 2084, 2084, 2088, 2088, 2112, 2136, 2208, 2208, 2210, 2220, 2308, 2361, 2365, 2365, 2384, 2384, 2392, 2401, 2417, 2423, 2425, 2431, 2437, 2444, 2447, 2448, 2451, 2472, 2474, 2480, 2482, 2482, 2486, 2489, 2493, 2493, 2510, 2510, 2524, 2525, 2527, 2529, 2544, 2545, 2565, 2570, 2575, 2576, 2579, 2600, 2602, 2608, 2610, 2611, 2613, 2614, 2616, 2617, 2649, 2652, 2654, 2654, 2674, 2676, 2693, 2701, 2703, 2705, 2707, 2728, 2730, 2736, 2738, 2739, 2741, 2745, 2749, 2749, 2768, 2768, 2784, 2785, 2821, 2828, 2831, 2832, 2835, 2856, 2858, 2864, 2866, 2867, 2869, 2873, 2877, 2877, 2908, 2909, 2911, 2913, 2929, 2929, 2947, 2947, 2949, 2954, 2958, 2960, 2962, 2965, 2969, 2970, 2972, 2972, 2974, 2975, 2979, 2980, 2984, 2986, 2990, 3001, 3024, 3024, 3077, 3084, 3086, 3088, 3090, 3112, 3114, 3123, 3125, 3129, 3133, 3133, 3160, 3161, 3168, 3169, 3205, 3212, 3214, 3216, 3218, 3240, 3242, 3251, 3253, 3257, 3261, 3261, 3294, 3294, 3296, 3297, 3313, 3314, 3333, 3340, 3342, 3344, 3346, 3386, 3389, 3389, 3406, 3406, 3424, 3425, 3450, 3455, 3461, 3478, 3482, 3505, 3507, 3515, 3517, 3517, 3520, 3526, 3585, 3632, 3634, 3635, 3648, 3654, 3713, 3714, 3716, 3716, 3719, 3720, 3722, 3722, 3725, 3725, 3732, 3735, 3737, 3743, 3745, 3747, 3749, 3749, 3751, 3751, 3754, 3755, 3757, 3760, 3762, 3763, 3773, 3773, 3776, 3780, 3782, 3782, 3804, 3807, 3840, 3840, 3904, 3911, 3913, 3948, 3976, 3980, 4096, 4138, 4159, 4159, 4176, 4181, 4186, 4189, 4193, 4193, 4197, 4198, 4206, 4208, 4213, 4225, 4238, 4238, 4256, 4293, 4295, 4295, 4301, 4301, 4304, 4346, 4348, 4680, 4682, 4685, 4688, 4694, 4696, 4696, 4698, 4701, 4704, 4744, 4746, 4749, 4752, 4784, 4786, 4789, 4792, 4798, 4800, 4800, 4802, 4805, 4808, 4822, 4824, 4880, 4882, 4885, 4888, 4954, 4992, 5007, 5024, 5108, 5121, 5740, 5743, 5759, 5761, 5786, 5792, 5866, 5870, 5872, 5888, 5900, 5902, 5905, 5920, 5937, 5952, 5969, 5984, 5996, 5998, 6000, 6016, 6067, 6103, 6103, 6108, 6108, 6176, 6263, 6272, 6312, 6314, 6314, 6320, 6389, 6400, 6428, 6480, 6509, 6512, 6516, 6528, 6571, 6593, 6599, 6656, 6678, 6688, 6740, 6823, 6823, 6917, 6963, 6981, 6987, 7043, 7072, 7086, 7087, 7098, 7141, 7168, 7203, 7245, 7247, 7258, 7293, 7401, 7404, 7406, 7409, 7413, 7414, 7424, 7615, 7680, 7957, 7960, 7965, 7968, 8005, 8008, 8013, 8016, 8023, 8025, 8025, 8027, 8027, 8029, 8029, 8031, 8061, 8064, 8116, 8118, 8124, 8126, 8126, 8130, 8132, 8134, 8140, 8144, 8147, 8150, 8155, 8160, 8172, 8178, 8180, 8182, 8188, 8305, 8305, 8319, 8319, 8336, 8348, 8450, 8450, 8455, 8455, 8458, 8467, 8469, 8469, 8473, 8477, 8484, 8484, 8486, 8486, 8488, 8488, 8490, 8493, 8495, 8505, 8508, 8511, 8517, 8521, 8526, 8526, 8544, 8584, 11264, 11310, 11312, 11358, 11360, 11492, 11499, 11502, 11506, 11507, 11520, 11557, 11559, 11559, 11565, 11565, 11568, 11623, 11631, 11631, 11648, 11670, 11680, 11686, 11688, 11694, 11696, 11702, 11704, 11710, 11712, 11718, 11720, 11726, 11728, 11734, 11736, 11742, 11823, 11823, 12293, 12295, 12321, 12329, 12337, 12341, 12344, 12348, 12353, 12438, 12445, 12447, 12449, 12538, 12540, 12543, 12549, 12589, 12593, 12686, 12704, 12730, 12784, 12799, 13312, 19893, 19968, 40908, 40960, 42124, 42192, 42237, 42240, 42508, 42512, 42527, 42538, 42539, 42560, 42606, 42623, 42647, 42656, 42735, 42775, 42783, 42786, 42888, 42891, 42894, 42896, 42899, 42912, 42922, 43000, 43009, 43011, 43013, 43015, 43018, 43020, 43042, 43072, 43123, 43138, 43187, 43250, 43255, 43259, 43259, 43274, 43301, 43312, 43334, 43360, 43388, 43396, 43442, 43471, 43471, 43520, 43560, 43584, 43586, 43588, 43595, 43616, 43638, 43642, 43642, 43648, 43695, 43697, 43697, 43701, 43702, 43705, 43709, 43712, 43712, 43714, 43714, 43739, 43741, 43744, 43754, 43762, 43764, 43777, 43782, 43785, 43790, 43793, 43798, 43808, 43814, 43816, 43822, 43968, 44002, 44032, 55203, 55216, 55238, 55243, 55291, 63744, 64109, 64112, 64217, 64256, 64262, 64275, 64279, 64285, 64285, 64287, 64296, 64298, 64310, 64312, 64316, 64318, 64318, 64320, 64321, 64323, 64324, 64326, 64433, 64467, 64829, 64848, 64911, 64914, 64967, 65008, 65019, 65136, 65140, 65142, 65276, 65313, 65338, 65345, 65370, 65382, 65470, 65474, 65479, 65482, 65487, 65490, 65495, 65498, 65500, };
        public static int[] UnicodeEs5IdentifierPart = { 170, 170, 181, 181, 186, 186, 192, 214, 216, 246, 248, 705, 710, 721, 736, 740, 748, 748, 750, 750, 768, 884, 886, 887, 890, 893, 902, 902, 904, 906, 908, 908, 910, 929, 931, 1013, 1015, 1153, 1155, 1159, 1162, 1319, 1329, 1366, 1369, 1369, 1377, 1415, 1425, 1469, 1471, 1471, 1473, 1474, 1476, 1477, 1479, 1479, 1488, 1514, 1520, 1522, 1552, 1562, 1568, 1641, 1646, 1747, 1749, 1756, 1759, 1768, 1770, 1788, 1791, 1791, 1808, 1866, 1869, 1969, 1984, 2037, 2042, 2042, 2048, 2093, 2112, 2139, 2208, 2208, 2210, 2220, 2276, 2302, 2304, 2403, 2406, 2415, 2417, 2423, 2425, 2431, 2433, 2435, 2437, 2444, 2447, 2448, 2451, 2472, 2474, 2480, 2482, 2482, 2486, 2489, 2492, 2500, 2503, 2504, 2507, 2510, 2519, 2519, 2524, 2525, 2527, 2531, 2534, 2545, 2561, 2563, 2565, 2570, 2575, 2576, 2579, 2600, 2602, 2608, 2610, 2611, 2613, 2614, 2616, 2617, 2620, 2620, 2622, 2626, 2631, 2632, 2635, 2637, 2641, 2641, 2649, 2652, 2654, 2654, 2662, 2677, 2689, 2691, 2693, 2701, 2703, 2705, 2707, 2728, 2730, 2736, 2738, 2739, 2741, 2745, 2748, 2757, 2759, 2761, 2763, 2765, 2768, 2768, 2784, 2787, 2790, 2799, 2817, 2819, 2821, 2828, 2831, 2832, 2835, 2856, 2858, 2864, 2866, 2867, 2869, 2873, 2876, 2884, 2887, 2888, 2891, 2893, 2902, 2903, 2908, 2909, 2911, 2915, 2918, 2927, 2929, 2929, 2946, 2947, 2949, 2954, 2958, 2960, 2962, 2965, 2969, 2970, 2972, 2972, 2974, 2975, 2979, 2980, 2984, 2986, 2990, 3001, 3006, 3010, 3014, 3016, 3018, 3021, 3024, 3024, 3031, 3031, 3046, 3055, 3073, 3075, 3077, 3084, 3086, 3088, 3090, 3112, 3114, 3123, 3125, 3129, 3133, 3140, 3142, 3144, 3146, 3149, 3157, 3158, 3160, 3161, 3168, 3171, 3174, 3183, 3202, 3203, 3205, 3212, 3214, 3216, 3218, 3240, 3242, 3251, 3253, 3257, 3260, 3268, 3270, 3272, 3274, 3277, 3285, 3286, 3294, 3294, 3296, 3299, 3302, 3311, 3313, 3314, 3330, 3331, 3333, 3340, 3342, 3344, 3346, 3386, 3389, 3396, 3398, 3400, 3402, 3406, 3415, 3415, 3424, 3427, 3430, 3439, 3450, 3455, 3458, 3459, 3461, 3478, 3482, 3505, 3507, 3515, 3517, 3517, 3520, 3526, 3530, 3530, 3535, 3540, 3542, 3542, 3544, 3551, 3570, 3571, 3585, 3642, 3648, 3662, 3664, 3673, 3713, 3714, 3716, 3716, 3719, 3720, 3722, 3722, 3725, 3725, 3732, 3735, 3737, 3743, 3745, 3747, 3749, 3749, 3751, 3751, 3754, 3755, 3757, 3769, 3771, 3773, 3776, 3780, 3782, 3782, 3784, 3789, 3792, 3801, 3804, 3807, 3840, 3840, 3864, 3865, 3872, 3881, 3893, 3893, 3895, 3895, 3897, 3897, 3902, 3911, 3913, 3948, 3953, 3972, 3974, 3991, 3993, 4028, 4038, 4038, 4096, 4169, 4176, 4253, 4256, 4293, 4295, 4295, 4301, 4301, 4304, 4346, 4348, 4680, 4682, 4685, 4688, 4694, 4696, 4696, 4698, 4701, 4704, 4744, 4746, 4749, 4752, 4784, 4786, 4789, 4792, 4798, 4800, 4800, 4802, 4805, 4808, 4822, 4824, 4880, 4882, 4885, 4888, 4954, 4957, 4959, 4992, 5007, 5024, 5108, 5121, 5740, 5743, 5759, 5761, 5786, 5792, 5866, 5870, 5872, 5888, 5900, 5902, 5908, 5920, 5940, 5952, 5971, 5984, 5996, 5998, 6000, 6002, 6003, 6016, 6099, 6103, 6103, 6108, 6109, 6112, 6121, 6155, 6157, 6160, 6169, 6176, 6263, 6272, 6314, 6320, 6389, 6400, 6428, 6432, 6443, 6448, 6459, 6470, 6509, 6512, 6516, 6528, 6571, 6576, 6601, 6608, 6617, 6656, 6683, 6688, 6750, 6752, 6780, 6783, 6793, 6800, 6809, 6823, 6823, 6912, 6987, 6992, 7001, 7019, 7027, 7040, 7155, 7168, 7223, 7232, 7241, 7245, 7293, 7376, 7378, 7380, 7414, 7424, 7654, 7676, 7957, 7960, 7965, 7968, 8005, 8008, 8013, 8016, 8023, 8025, 8025, 8027, 8027, 8029, 8029, 8031, 8061, 8064, 8116, 8118, 8124, 8126, 8126, 8130, 8132, 8134, 8140, 8144, 8147, 8150, 8155, 8160, 8172, 8178, 8180, 8182, 8188, 8204, 8205, 8255, 8256, 8276, 8276, 8305, 8305, 8319, 8319, 8336, 8348, 8400, 8412, 8417, 8417, 8421, 8432, 8450, 8450, 8455, 8455, 8458, 8467, 8469, 8469, 8473, 8477, 8484, 8484, 8486, 8486, 8488, 8488, 8490, 8493, 8495, 8505, 8508, 8511, 8517, 8521, 8526, 8526, 8544, 8584, 11264, 11310, 11312, 11358, 11360, 11492, 11499, 11507, 11520, 11557, 11559, 11559, 11565, 11565, 11568, 11623, 11631, 11631, 11647, 11670, 11680, 11686, 11688, 11694, 11696, 11702, 11704, 11710, 11712, 11718, 11720, 11726, 11728, 11734, 11736, 11742, 11744, 11775, 11823, 11823, 12293, 12295, 12321, 12335, 12337, 12341, 12344, 12348, 12353, 12438, 12441, 12442, 12445, 12447, 12449, 12538, 12540, 12543, 12549, 12589, 12593, 12686, 12704, 12730, 12784, 12799, 13312, 19893, 19968, 40908, 40960, 42124, 42192, 42237, 42240, 42508, 42512, 42539, 42560, 42607, 42612, 42621, 42623, 42647, 42655, 42737, 42775, 42783, 42786, 42888, 42891, 42894, 42896, 42899, 42912, 42922, 43000, 43047, 43072, 43123, 43136, 43204, 43216, 43225, 43232, 43255, 43259, 43259, 43264, 43309, 43312, 43347, 43360, 43388, 43392, 43456, 43471, 43481, 43520, 43574, 43584, 43597, 43600, 43609, 43616, 43638, 43642, 43643, 43648, 43714, 43739, 43741, 43744, 43759, 43762, 43766, 43777, 43782, 43785, 43790, 43793, 43798, 43808, 43814, 43816, 43822, 43968, 44010, 44012, 44013, 44016, 44025, 44032, 55203, 55216, 55238, 55243, 55291, 63744, 64109, 64112, 64217, 64256, 64262, 64275, 64279, 64285, 64296, 64298, 64310, 64312, 64316, 64318, 64318, 64320, 64321, 64323, 64324, 64326, 64433, 64467, 64829, 64848, 64911, 64914, 64967, 65008, 65019, 65024, 65039, 65056, 65062, 65075, 65076, 65101, 65103, 65136, 65140, 65142, 65276, 65296, 65305, 65313, 65338, 65343, 65343, 65345, 65370, 65382, 65470, 65474, 65479, 65482, 65487, 65490, 65495, 65498, 65500, };
        public static int MergeConflictMarkerLength = "<<<<<<<".Length;

        public static readonly Regex DirectiveRegex = new Regex("^#[a-z]+");
        public static readonly Regex ShebangTriviaRegex = new Regex("^#!.*");

        public static int CharCodeAt(string text, int pos)
        {
            return pos < text.Length ? (int)text[pos] : -1;
        }

        public static string StringFromCharCode(params int[] codes)
        {
            var sb = new StringBuilder();
            foreach (var c in codes)
            {
                sb.Append((char)c);
            }
            return sb.ToString();
        }

        public static SyntaxKind GetIdentifierToken(string text, Dictionary<string, SyntaxKind> tokenMap)
        {
            // var len = _tokenValue.Length;
            // if (len >= 2 && len <= 11)
            // {
                var ch = CharCodeAt(text, 0);
                if (ch >= (int)CharacterCodes.a && ch <= (int)CharacterCodes.z)
                {
                    if (tokenMap.ContainsKey(text))
                    {
                        return tokenMap[text];
                    }
                }
            // }

            return SyntaxKind.Identifier;
        }

        public static bool IsConflictMarkerTrivia(string text, int pos)
        {
            Debug.Assert(pos >= 0);
            if (pos == 0 || IsLineBreak(CharCodeAt(text, pos - 1)))
            {
                var ch = CharCodeAt(text, pos);
                if ((pos + MergeConflictMarkerLength) < text.Length)
                {
                    for (var i = 0; i < MergeConflictMarkerLength; i++)
                    {
                        if (CharCodeAt(text, pos + i) != ch)
                        {
                            return false;
                        }
                    };
                    return ch == (int)CharacterCodes.Equals ||
                                        CharCodeAt(text, pos + MergeConflictMarkerLength) == (int)CharacterCodes.Space;
                }
            }
            return false;
        }

        public static Result<Lexer.XToken> ReScanGreaterThanToken(Lexer.XToken token, Lexer lexer)
        {
            var result = new Result<Lexer.XToken>()
            {
                Value = token
            };

            var text = lexer.SourceText;

            // [dho] ensure lexer is aligned to correct position to start - 30/03/19
            lexer.Pos = token.StartPos;
            
            if (token.Kind == SyntaxKind.GreaterThanToken)
            {   
                lexer.Pos += 1;
                if (CharCodeAt(text, lexer.Pos) == (int)CharacterCodes.GreaterThan)
                {
                    if (CharCodeAt(text, lexer.Pos + 1) == (int)CharacterCodes.GreaterThan)
                    {
                        if (CharCodeAt(text, lexer.Pos + 2) == (int)CharacterCodes.Equals)
                        {
                            lexer.Pos += 3;

                            result.Value = MakeToken(text, token.StartPos, lexer.Pos, SyntaxKind.GreaterThanGreaterThanGreaterThanEqualsToken);

                            return result;
                        }

                        lexer.Pos += 2;

                        result.Value = MakeToken(text, token.StartPos, lexer.Pos, SyntaxKind.GreaterThanGreaterThanGreaterThanToken);

                        return result;
                    }
                    if (CharCodeAt(text, lexer.Pos + 1) == (int)CharacterCodes.Equals)
                    {
                        lexer.Pos += 2;

                        result.Value = MakeToken(text, token.StartPos, lexer.Pos, SyntaxKind.GreaterThanGreaterThanEqualsToken);

                        return result;
                    }
                    lexer.Pos++;

                    result.Value = MakeToken(text, token.StartPos, lexer.Pos, SyntaxKind.GreaterThanGreaterThanToken);

                    return result;
                }
                if (CharCodeAt(text, lexer.Pos) == (int)CharacterCodes.Equals)
                {
                    lexer.Pos++;

                    result.Value = MakeToken(text, token.StartPos, lexer.Pos, SyntaxKind.GreaterThanEqualsToken);

                    return result;
                }
            }
            else
            {
                result.AddMessages(
                    new ParsingMessage(MessageKind.Error, "Expected '>'", new Range(token.StartPos, lexer.Pos))
                );
            }
            
            return result;
        }

        private static Lexer.XToken MakeToken(string text, int startPos, int endPos, SyntaxKind kind)
        {
            var lexeme = text.Substring(startPos, endPos - startPos);

            return new Lexer.XToken()
            {
                Kind = kind,
                Lexeme = lexeme,
                StartPos = startPos,
            };
        }

        public static Result<Lexer.XToken> ReScanSlashToken(Lexer.XToken token, Lexer lexer)
        {
            var result = new Result<Lexer.XToken>();

            // [dho] ensure lexer is aligned to correct position to start - 30/03/19
            lexer.Pos = token.StartPos;

            if (token.Kind == SyntaxKind.SlashToken || token.Kind == SyntaxKind.SlashEqualsToken)
            {
                lexer.Pos += 1;

                var text = lexer.SourceText;
                var inEscape = false;
                var inCharacterClass = false;
                var isUnterminated = false;

                while (true)
                {
                    if (lexer.Pos >= text.Length)
                    {
                        isUnterminated = true;
                        result.AddMessages(
                            new ParsingMessage(MessageKind.Error, "Unterminated regular expression literal", new Range(token.StartPos, lexer.Pos))
                        );
                        break;
                    }

                    var ch = CharCodeAt(text, lexer.Pos);

                    if (IsLineBreak(ch))
                    {
                        isUnterminated = true;
                        result.AddMessages(
                            new ParsingMessage(MessageKind.Error, "Unterminated regular expression literal", new Range(token.StartPos, lexer.Pos))
                        );
                        break;
                    }

                    if (inEscape)
                    {
                        // Parsing an escape character;
                        // reset the flag and just advance to the next char.
                        inEscape = false;
                    }
                    else if (ch == (int)CharacterCodes.Slash && !inCharacterClass)
                    {
                        // A slash within a character class is permissible,
                        // but in general it signals the end of the regexp literal.
                        lexer.Pos++;
                        break;
                    }
                    else if (ch == (int)CharacterCodes.OpenBracket)
                    {
                        inCharacterClass = true;
                    }
                    else if (ch == (int)CharacterCodes.Backslash)
                    {
                        inEscape = true;
                    }
                    else if (ch == (int)CharacterCodes.CloseBracket)
                    {
                        inCharacterClass = false;
                    }

                    lexer.Pos++;
                }
                while (lexer.Pos < text.Length && IsIdentifierPart(CharCodeAt(text, lexer.Pos)/* , _languageVersion*/))
                {
                    lexer.Pos++;
                }
                
                var lexeme = text.Substring(token.StartPos, lexer.Pos - token.StartPos);

                result.Value = new Lexer.XToken()
                {
                    Kind = SyntaxKind.RegularExpressionLiteral,
                    Lexeme = lexeme,
                    StartPos = token.StartPos,
                };
            }
            else
            {
                result.Value = token;
            }

            return result;
        }

        public static Result<Lexer.XToken> ReScanTemplateToken(Lexer.XToken token, Lexer lexer)
        {
            // [dho] ensure lexer is aligned to correct position to start - 30/03/19
            lexer.Pos = token.StartPos;

            if(token.Kind == SyntaxKind.CloseBraceToken)
            {
                return lexer.ScanTemplateAndSetTokenValue();
            }
            else
            {
                var result = new Result<Lexer.XToken>();
                
                result.AddMessages(
                    new ParsingMessage(MessageKind.Error, "'reScanTemplateToken' should only be called on a '}'", new Range(token.StartPos, token.StartPos + 1))
                );

                return result;
            }
        }

        public static Result<Lexer.XToken> ReScanJSXToken(Lexer.XToken token, Lexer lexer)
        {
            lexer.Pos = token.StartPos;

            return lexer.ScanJSXToken();
        }

        public static bool IsDigit(int ch)
        {
            return ch >= (int)CharacterCodes._0 && ch <= (int)CharacterCodes._9;
        }

        public static bool IsOctalDigit(int ch)
        {
            return ch >= (int)CharacterCodes._0 && ch <= (int)CharacterCodes._7;
        }

        public static bool IsIdentifierStart(int ch/* , ScriptTarget languageVersion*/)
        {
            return ch >= (int)CharacterCodes.A && ch <= (int)CharacterCodes.Z || ch >= (int)CharacterCodes.a && ch <= (int)CharacterCodes.z ||
                        ch == (int)CharacterCodes.Dollar || ch == (int)CharacterCodes._ ||
                        ch > (int)CharacterCodes.MaxAsciiCharacter && IsUnicodeIdentifierStart(ch/*, languageVersion */);
        }


        public static bool IsIdentifierPart(int ch/*, ScriptTarget languageVersion */)
        {
            return ch >= (int)CharacterCodes.A && ch <= (int)CharacterCodes.Z || ch >= (int)CharacterCodes.a && ch <= (int)CharacterCodes.z ||
                        ch >= (int)CharacterCodes._0 && ch <= (int)CharacterCodes._9 || ch == (int)CharacterCodes.Dollar || ch == (int)CharacterCodes._ ||
                        ch > (int)CharacterCodes.MaxAsciiCharacter && IsUnicodeIdentifierPart(ch/*, languageVersion */);
        }

        public static bool IsShebangTrivia(string text, int pos)
        {
            // Shebangs check must only be done at the start of the file
            Debug.Assert(pos == 0);
            return ShebangTriviaRegex.IsMatch(text);
        }

        public static bool IsDirective(string text, int pos)
        {
            return DirectiveRegex.IsMatch(text.Substring(pos));
        }

        public static bool IsUnicodeIdentifierStart(int code/* , ScriptTarget languageVersion*/)
        {
            return //languageVersion >= ScriptTarget.Es5 ?
                        LookupInUnicodeMap(code, UnicodeEs5IdentifierStart); //:
                        //LookupInUnicodeMap(code, UnicodeEs3IdentifierStart);
        }


        public static bool IsUnicodeIdentifierPart(int code/*, ScriptTarget languageVersion */)
        {
            return //languageVersion >= ScriptTarget.Es5 ?
                        LookupInUnicodeMap(code, UnicodeEs5IdentifierPart); //:
                        //LookupInUnicodeMap(code, UnicodeEs3IdentifierPart);
        }

        public static bool LookupInUnicodeMap(int code, int[] map)
        {
            if (code < map[0])
            {
                return false;
            }
            var lo = 0;
            int hi = map.Length;
            int mid = 0;
            while (lo + 1 < hi)
            {
                mid = lo + (hi - lo) / 2;
                // mid has to be even to catch a range's beginning
                mid -= mid % 2;
                if (map[mid] <= code && code <= map[mid + 1])
                {
                    return true;
                }
                if (code < map[mid])
                {
                    hi = mid;
                }
                else
                {
                    lo = mid + 2;
                }
            }
            return false;
        }

        public static bool IsLineBreak(int ch)
        {
            // ES5 7.3:
            // The ECMAScript line terminator characters are listed in Table 3.
            //     Table 3: Line Terminator Characters
            //     Code Unit Value     Name                    Formal Name
            //     \u000A              Line Feed               <LF>
            //     \u000D              Carriage Return         <CR>
            //     \u2028              Line separator          <LS>
            //     \u2029              Paragraph separator     <PS>
            // Only the characters in Table 3 are treated as line terminators. Other new line or line
            // breaking characters are treated as white space but not as line terminators.

            return ch == (int)CharacterCodes.LineFeed ||
                ch == (int)CharacterCodes.CarriageReturn ||
                ch == (int)CharacterCodes.LineSeparator ||
                ch == (int)CharacterCodes.ParagraphSeparator;
        }

        public static bool IsWhiteSpace(int ch)
        {
            return IsWhiteSpaceSingleLine(ch) || IsLineBreak(ch);
        }


        public static bool IsWhiteSpaceSingleLine(int ch)
        {
            // Note: nextLine is in the Zs space, and should be considered to be a whitespace.
            // It is explicitly not a line-break as it isn't in the exact set specified by EcmaScript.
            return ch == (int)CharacterCodes.Space ||
                ch == (int)CharacterCodes.Tab ||
                ch == (int)CharacterCodes.VerticalTab ||
                ch == (int)CharacterCodes.FormFeed ||
                ch == (int)CharacterCodes.NonBreakingSpace ||
                ch == (int)CharacterCodes.NextLine ||
                ch == (int)CharacterCodes.Ogham ||
                ch >= (int)CharacterCodes.EnQuad && ch <= (int)CharacterCodes.ZeroWidthSpace ||
                ch == (int)CharacterCodes.NarrowNoBreakSpace ||
                ch == (int)CharacterCodes.MathematicalSpace ||
                ch == (int)CharacterCodes.IdeographicSpace ||
                ch == (int)CharacterCodes.ByteOrderMark;
        }
    }

    public class Lexer
    {
        
        private static readonly int _mergeConflictMarkerLength = "<<<<<<<".Length;

        public class XToken
        {
            // private int LineStartPos;
            public bool PrecedingLineBreak;

            public int StartPos;

            public SyntaxKind Kind;

            public string Lexeme;
        }

        public readonly string SourceText;

        public readonly List<int> LineStarts;

        public Dictionary<string, SyntaxKind> TokenMap { get; set; }

        public int Pos { get; set; }

        // public LanguageVariant LanguageVariant { get; set; }

        public Lexer(string sourceText, int pos = 0)
        {
            SourceText = sourceText;
            Pos = pos;
            TokenMap = S1.DefaultTokenMap;
            LineStarts = ComputeLineStarts(SourceText);
        }

        // [dho] clone constructor - 09/02/19
        private Lexer(string sourceText, int pos, Dictionary<string, SyntaxKind> tokenMap, List<int> lineStarts)
        {
            SourceText = sourceText;
            Pos = pos;
            TokenMap = tokenMap;
            LineStarts = lineStarts;
        }

        public Lexer Clone()
        {
            return new Lexer(SourceText, Pos, TokenMap, LineStarts);
        }


        public (int, int) GetLineAndCharacterOfPosition(int position)
        {
            var lineNumber = BinarySearch(LineStarts, position);
            if (lineNumber < 0)
            {
                // If the actual position was not found,
                // the binary search returns the 2's-complement of the next line start
                // e.g. if the line starts at [5, 10, 23, 80] and the position requested was 20
                // then the search will return -2.
                //
                // We want the index of the previous line start, so we subtract 1.
                // Review 2's-complement if this is confusing.
                lineNumber = ~lineNumber - 1;
                // Debug.Assert(lineNumber != -1, "position cannot precede the beginning of the file");
            }

            var character = position - LineStarts[lineNumber];

            return (lineNumber, character);
        }

        // [dho] TODO move
        public static int BinarySearch(IList<int> array, int value, Func<int, int, int> comparer = null, int? offset = null)
        {
            if (array == null || array.Count == 0)
            {
                return -1;
            }
            var low = offset ?? 0;
            var high = array.Count - 1;
            comparer = comparer ?? ((v1, v2) => (v1 < v2 ? -1 : (v1 > v2 ? 1 : 0)));
            while (low <= high)
            {
                var middle = low + ((high - low) >> 1);
                var midValue = array[middle];
                if (comparer(midValue, value) == 0)
                {
                    return middle;
                }
                else
                if (comparer(midValue, value) > 0)
                {
                    high = middle - 1;
                }
                else
                {
                    low = middle + 1;
                }
            }
            return ~low;
        }

        public static List<int> ComputeLineStarts(string text)
        {
            List<int> result = new List<int>();
            
            var pos = 0;
            
            var lineStart = 0;

            while (pos < text.Length)
            {
                var ch = S1.CharCodeAt(text, pos);
                pos++;
                switch (ch)
                {
                    case (int)CharacterCodes.CarriageReturn:
                        if (S1.CharCodeAt(text, pos) == (int)CharacterCodes.LineFeed)
                        {
                            pos++;
                        }
                        goto caseLabel2;

                    case (int)CharacterCodes.LineFeed:
                        caseLabel2: result.Add(lineStart);
                        lineStart = pos;
                        break;
                    default:
                        if (ch > (int)CharacterCodes.MaxAsciiCharacter && S1.IsLineBreak(ch))
                        {
                            result.Add(lineStart);
                            lineStart = pos;
                        }
                        break;
                }
            }
            result.Add(lineStart);
            return result;
        }

        public Result<XToken> NextToken()
        {
            XToken token = new XToken();

            var result = new Result<XToken>()
            {
                // [dho] to make life easier in a messy algorithm
                // below we just set the result value up front, and it's
                // on the caller to check there were no errors before
                // using the value - 21/01/19
                Value = token
            };

            // var _skipTrivia = true; // [dho] TOIDO configurable? - 20/01/19
            // var _precedingLineBreak = false;

            var end = SourceText.Length;

            // _startPos = pos;
            // _hasExtendedUnicodeEscape = false;
            // _tokenIsUnterminated = false;


            while (true)
            {
                token.StartPos = Pos;
  
                if (Pos >= end)
                {
                    token.Kind = SyntaxKind.EndOfFileToken;
                    
                    return result;
                }
                var ch = S1.CharCodeAt(SourceText, Pos);

                if (ch == (int)CharacterCodes.Hash)
                {
                    if(S1.IsDirective(SourceText, Pos))
                    {
                        token.Lexeme = ScanDirective();

                        token.Kind = SyntaxKind.Directive;

                        return result;
                    }

                    if(Pos == 0 && S1.IsShebangTrivia(SourceText, Pos))
                    {
                        token.Lexeme = ScanShebangTrivia();

                        // if (_skipTrivia)
                        // {
                        //     continue;
                        // }
                        // else
                        // {
                            token.Kind = SyntaxKind.ShebangTrivia;
                            
                            return result;
                        // }
                    }


                }
                switch (ch)
                {
                    case (int)CharacterCodes.LineFeed:
                    case (int)CharacterCodes.CarriageReturn:
                        token.PrecedingLineBreak = true;
                        
                        
                        // [dho] skip whitespace triva for now - 01/03/19
                        Pos++;
                        continue;
                        
                        
                        // if (_skipTrivia)
                        // {
                        //     pos++;
                        //     continue;
                        // }
                        // else
                        // {
                            if (ch == (int)CharacterCodes.CarriageReturn && 
                                        Pos + 1 < end && 
                                        S1.CharCodeAt(SourceText, Pos + 1) == (int)CharacterCodes.LineFeed)
                            {
                                Pos += 2;
                            }
                            else
                            {
                                Pos++;
                            }

                            token.Kind = SyntaxKind.NewLineTrivia;

                            return result;
                        // }
                        // goto caseLabel6;
                    case (int)CharacterCodes.Tab:
                    case (int)CharacterCodes.VerticalTab:
                    case (int)CharacterCodes.FormFeed:
                    case (int)CharacterCodes.Space:
                        caseLabel6: {

                            // [dho] skip whitespace triva for now - 01/03/19
                            Pos++;
                            continue;

                        //     if (_skipTrivia)
                        // {
                        //     pos++;
                        //     continue;
                        // }
                        // else
                        // {
                            while (Pos < end && S1.IsWhiteSpaceSingleLine(S1.CharCodeAt(SourceText, Pos)))
                            {
                                Pos++;
                            }
                            
                            token.Kind = SyntaxKind.WhitespaceTrivia;
                            
                            

                            return result;
                        // }
                        // goto caseLabel7;
                        }
                    case (int)CharacterCodes.Exclamation:
                    caseLabel7: {
                        if (S1.CharCodeAt(SourceText, Pos + 1) == (int)CharacterCodes.Equals)
                        {
                            if (S1.CharCodeAt(SourceText, Pos + 2) == (int)CharacterCodes.Equals)
                            {
                                Pos += 3;
                                token.Kind = SyntaxKind.ExclamationEqualsEqualsToken;
                                
                                

                                return result;
                            }
                            Pos += 2;
                            token.Kind = SyntaxKind.ExclamationEqualsToken;
                            
                            

                            return result;
                        }
                        Pos++;
                        token.Kind = SyntaxKind.ExclamationToken;
                        
                        

                        return result;
                    }

                    case (int)CharacterCodes.DoubleQuote:
                    case (int)CharacterCodes.SingleQuote:{
                        
                        var (messages, lexeme) = ScanString(/* allowEscapes */ true);
                        
                        result.AddMessages(messages);

                        if(!HasErrors(result))
                        {
                            token.Lexeme = lexeme;
                            token.Kind = SyntaxKind.StringLiteral;
                            
                            
                        }

                        return result;
                    }

                    case (int)CharacterCodes.Backtick:{
                        
                        var (messages, t) = ScanTemplateAndSetTokenValue();

                        result.AddMessages(messages);

                        result.Value = t;

                        return result;
                    }

                    case (int)CharacterCodes.Percent:{
                        if (S1.CharCodeAt(SourceText, Pos + 1) == (int)CharacterCodes.Equals)
                        {
                            Pos += 2;
                            token.Kind = SyntaxKind.PercentEqualsToken;
                            
                            return result;
                        }

                        Pos++;
                        token.Kind = SyntaxKind.PercentToken;
                        
                        return result;
                    }

                    case (int)CharacterCodes.Ampersand:{
                        if (S1.CharCodeAt(SourceText, Pos + 1) == (int)CharacterCodes.Ampersand)
                        {
                            Pos += 2;
                            token.Kind = SyntaxKind.AmpersandAmpersandToken;

                            return result;
                        }
                        if (S1.CharCodeAt(SourceText, Pos + 1) == (int)CharacterCodes.Equals)
                        {
                            Pos += 2;
                            token.Kind = SyntaxKind.AmpersandEqualsToken;

                            return result;
                        }
                        Pos++;
                        token.Kind = SyntaxKind.AmpersandToken;
                        
                        return result;
                    }

                    case (int)CharacterCodes.OpenParen:{
                        Pos++;
                        token.Kind = SyntaxKind.OpenParenToken;
                        
                        return result;
                    }

                    case (int)CharacterCodes.CloseParen:{
                        Pos++;
                        token.Kind = SyntaxKind.CloseParenToken;
                        
                        return result;
                    }

                    case (int)CharacterCodes.Asterisk:{
                        if (S1.CharCodeAt(SourceText, Pos + 1) == (int)CharacterCodes.Equals)
                        {
                            Pos += 2;
                            token.Kind = SyntaxKind.AsteriskEqualsToken;
                            
                            return result;
                        }
                        if (S1.CharCodeAt(SourceText, Pos + 1) == (int)CharacterCodes.Asterisk)
                        {
                            if (S1.CharCodeAt(SourceText, Pos + 2) == (int)CharacterCodes.Equals)
                            {
                                Pos += 3;
                                token.Kind = SyntaxKind.AsteriskAsteriskEqualsToken;
 
                                return result;
                            }
                            Pos += 2;
                            token.Kind = SyntaxKind.AsteriskAsteriskToken;
 
                            return result;
                        }
                        Pos++;
                        token.Kind = SyntaxKind.AsteriskToken;

                        return result;
                    }
                    case (int)CharacterCodes.Plus:{
                        if (S1.CharCodeAt(SourceText, Pos + 1) == (int)CharacterCodes.Plus)
                        {
                            Pos += 2;
                            token.Kind = SyntaxKind.PlusPlusToken;
                            
                            return result;
                        }
                        if (S1.CharCodeAt(SourceText, Pos + 1) == (int)CharacterCodes.Equals)
                        {
                            Pos += 2;
                            token.Kind = SyntaxKind.PlusEqualsToken;
                            
                            

                            return result;
                        }
                        Pos++;
                        token.Kind = SyntaxKind.PlusToken;
                        
                        

                        return result;
                    }

                    case (int)CharacterCodes.Comma:{
                        Pos++;
                        token.Kind = SyntaxKind.CommaToken;
                        
                        

                        return result;
                    }

                    case (int)CharacterCodes.Minus:{
                        if (S1.CharCodeAt(SourceText, Pos + 1) == (int)CharacterCodes.Minus)
                        {
                            Pos += 2;
                            token.Kind = SyntaxKind.MinusMinusToken;
                            
                            

                            return result;
                        }
                        if (S1.CharCodeAt(SourceText, Pos + 1) == (int)CharacterCodes.Equals)
                        {
                            Pos += 2;
                            token.Kind = SyntaxKind.MinusEqualsToken;
                            
                            

                            return result;
                        }
                        Pos++;
                        token.Kind = SyntaxKind.MinusToken;
                        
                        

                        return result;
                    }

                    case (int)CharacterCodes.Dot:{
                        if (S1.IsDigit(S1.CharCodeAt(SourceText, Pos + 1)))
                        {
                            var (messages, lexeme) = ScanNumber();

                            result.AddMessages(messages);

                            token.Lexeme = lexeme;
                            token.Kind = SyntaxKind.NumericLiteral;

                            return result;
                        }
                        if (S1.CharCodeAt(SourceText, Pos + 1) == (int)CharacterCodes.Dot && 
                            S1.CharCodeAt(SourceText, Pos + 2) == (int)CharacterCodes.Dot)
                        {
                            Pos += 3;
                            token.Kind = SyntaxKind.DotDotDotToken;
                    
                            return result;
                        }
                        Pos++;
                        token.Kind = SyntaxKind.DotToken;
                        
                        

                        return result;
                    }

                    case (int)CharacterCodes.Slash:{
                        if (S1.CharCodeAt(SourceText, Pos + 1) == (int)CharacterCodes.Slash)
                        {
                            var commentStart = Pos;

                            Pos += 2;
                            while (Pos < end)
                            {
                                if (S1.IsLineBreak(S1.CharCodeAt(SourceText, Pos)))
                                {
                                    break;
                                }
                                Pos++;
                            }
                            // if (_skipTrivia)
                            // {
                            //     continue;
                            // }
                            // else
                            // {
                                token.Kind = SyntaxKind.SingleLineCommentTrivia;
                                
                                token.Lexeme = SourceText.Substring(commentStart, Pos - commentStart);

                                return result;
                            // }
                        }

                        if (S1.CharCodeAt(SourceText, Pos + 1) == (int)CharacterCodes.Asterisk)
                        {
                            var commentStart = Pos;

                            Pos += 2;
                            var commentClosed = false;
                            while (Pos < end)
                            {
                                var ch2 = S1.CharCodeAt(SourceText, Pos);
                                if (ch2 == (int)CharacterCodes.Asterisk && 
                                    S1.CharCodeAt(SourceText, Pos + 1) == (int)CharacterCodes.Slash)
                                {
                                    Pos += 2;
                                    commentClosed = true;
                                    break;
                                }
                                if (S1.IsLineBreak(ch2))
                                {
                                    token.PrecedingLineBreak = true;
                                }
                                Pos++;
                            }
                            
                            if (!commentClosed)
                            {
                                result.AddMessages(
                                    new ParsingMessage(MessageKind.Error, "'*/' expected", new Range(this.Pos, this.Pos))
                                );
                            }

                            // if (_skipTrivia)
                            // {
                            //     continue;
                            // }
                            // else
                            // {
                                // _tokenIsUnterminated = !commentClosed;
                                // if(!commentClosed)
                                // {
                                //     result.AddMessages(
                                //         new ParsingMessage(MessageKind.Error, "Unterminated multiline comment", new Range(Pos, Pos))
                                //     );
                                // }

                                token.Kind = SyntaxKind.MultiLineCommentTrivia;

                                token.Lexeme = SourceText.Substring(commentStart, Pos - commentStart);

                                return result;
                            // }
                        }
                        if (S1.CharCodeAt(SourceText, Pos + 1) == (int)CharacterCodes.Equals)
                        {
                            Pos += 2;
                            token.Kind = SyntaxKind.SlashEqualsToken;
                            
                            return result;
                        }
                        Pos++;
                        token.Kind = SyntaxKind.SlashToken;
                        

                        return result;
                    }

                    case (int)CharacterCodes._0:{
                        if (Pos + 2 < end && (S1.CharCodeAt(SourceText, Pos + 1) == (int)CharacterCodes.X || S1.CharCodeAt(SourceText, Pos + 1) == (int)CharacterCodes.x))
                        {
                            Pos += 2;
                            var value = ScanMinimumNumberOfHexDigits(1);
                            if (value < 0)
                            {
                                result.AddMessages(
                                    new ParsingMessage(MessageKind.Error, "Hexadecimal digit expected", new Range(this.Pos, this.Pos))
                                );

                                value = 0;
                            }

                            token.Lexeme = "" + value;
                            token.Kind = SyntaxKind.NumericLiteral;

                            return result;
                        }
                        else if (Pos + 2 < end && (S1.CharCodeAt(SourceText, Pos + 1) == (int)CharacterCodes.B || S1.CharCodeAt(SourceText, Pos + 1) == (int)CharacterCodes.b))
                        {
                            Pos += 2;
                            var value = ScanBinaryOrOctalDigits(/* base */ 2);
                            if (value < 0)
                            {
                                result.AddMessages(
                                    new ParsingMessage(MessageKind.Error, "Binary digit expected", new Range(this.Pos, this.Pos))
                                );

                                value = 0;
                            }
                            token.Lexeme = "" + value;
                            token.Kind = SyntaxKind.NumericLiteral;
                            
                            return result;
                        }
                        else if (Pos + 2 < end && (S1.CharCodeAt(SourceText, Pos + 1) == (int)CharacterCodes.O || S1.CharCodeAt(SourceText, Pos + 1) == (int)CharacterCodes.o))
                        {
                            Pos += 2;
                            var value = ScanBinaryOrOctalDigits(/* base */ 8);
                            if (value < 0)
                            {
                                result.AddMessages(
                                    new ParsingMessage(MessageKind.Error, "Octal digit expected", new Range(this.Pos, this.Pos))
                                );

                                value = 0;
                            }

                            token.Lexeme = "" + value;
                            token.Kind = SyntaxKind.NumericLiteral;
                            
                            return result;
                        }
                        else if (Pos + 1 < end && S1.IsOctalDigit(S1.CharCodeAt(SourceText, Pos + 1)))
                        {
                            token.Lexeme = "" + ScanOctalDigits();
                            token.Kind = SyntaxKind.NumericLiteral;

                            return result;
                        }
                    }
                    goto caseLabel30;

                    case (int)CharacterCodes._1:
                    case (int)CharacterCodes._2:
                    case (int)CharacterCodes._3:
                    case (int)CharacterCodes._4:
                    case (int)CharacterCodes._5:
                    case (int)CharacterCodes._6:
                    case (int)CharacterCodes._7:
                    case (int)CharacterCodes._8:
                    case (int)CharacterCodes._9:
                    caseLabel30:{
                        var (messages, lexeme) = ScanNumber();

                        result.AddMessages(messages);

                        token.Kind = SyntaxKind.NumericLiteral;
                        token.Lexeme = lexeme;
                        
                        return result;
                    }

                    case (int)CharacterCodes.Colon:{
                        Pos++;
                        token.Kind = SyntaxKind.ColonToken;
                    
                        return result;
                    }

                    case (int)CharacterCodes.Semicolon:{
                        Pos++;
                        token.Kind = SyntaxKind.SemicolonToken;
                        
                        

                        return result;
                    }

                    case (int)CharacterCodes.LessThan:{
                        if (S1.IsConflictMarkerTrivia(SourceText, Pos))
                        {
                            result.AddMessages(ScanConflictMarkerTrivia());

                            // if (_skipTrivia)
                            // {
                            //     continue;
                            // }
                            // else
                            // {
                                token.Kind = SyntaxKind.ConflictMarkerTrivia;
                                
                                return result;
                            // }
                        }
                        if (S1.CharCodeAt(SourceText, Pos + 1) == (int)CharacterCodes.LessThan)
                        {
                            if (S1.CharCodeAt(SourceText, Pos + 2) == (int)CharacterCodes.Equals)
                            {
                                Pos += 3;
                                token.Kind = SyntaxKind.LessThanLessThanEqualsToken;

                                return result;
                            }
                            Pos += 2;
                            token.Kind = SyntaxKind.LessThanLessThanToken;
                            
                            

                            return result;
                        }
                        if (S1.CharCodeAt(SourceText, Pos + 1) == (int)CharacterCodes.Equals)
                        {
                            Pos += 2;
                            token.Kind = SyntaxKind.LessThanEqualsToken;
                            
                            

                            return result;
                        }
                        if (//LanguageVariant == LanguageVariant.Jsx &&
                                                        S1.CharCodeAt(SourceText, Pos + 1) == (int)CharacterCodes.Slash &&
                                                        S1.CharCodeAt(SourceText, Pos + 2) != (int)CharacterCodes.Asterisk)
                        {   
                            Pos += 2;
                            token.Kind = SyntaxKind.LessThanSlashToken;
                            
                            

                            return result;
                        }
                        Pos++;
                        token.Kind = SyntaxKind.LessThanToken;
                        

                        return result;
                    }

                    case (int)CharacterCodes.Equals:{
                        if (S1.IsConflictMarkerTrivia(SourceText, Pos))
                        {
                            var (messages, _) = ScanConflictMarkerTrivia();

                            result.AddMessages(messages);

                            // if (_skipTrivia)
                            // {
                            //     continue;
                            // }
                            // else
                            // {
                                token.Kind = SyntaxKind.ConflictMarkerTrivia;

                                return result;
                            // }
                        }
                        if (S1.CharCodeAt(SourceText, Pos + 1) == (int)CharacterCodes.Equals)
                        {
                            if (S1.CharCodeAt(SourceText, Pos + 2) == (int)CharacterCodes.Equals)
                            {
                                Pos += 3;
                                token.Kind = SyntaxKind.EqualsEqualsEqualsToken;
                                
                                

                                return result;
                            }
                            Pos += 2;
                            token.Kind = SyntaxKind.EqualsEqualsToken;
                            
                            

                            return result;
                        }
                        if (S1.CharCodeAt(SourceText, Pos + 1) == (int)CharacterCodes.GreaterThan)
                        {
                            Pos += 2;
                            token.Kind = SyntaxKind.EqualsGreaterThanToken;
                            
                            

                            return result;
                        }
                        Pos++;
                        token.Kind = SyntaxKind.EqualsToken;
                        
                        

                        return result;
                    }

                    case (int)CharacterCodes.GreaterThan:{
                        if (S1.IsConflictMarkerTrivia(SourceText, Pos))
                        {
                            var (messages, _) = ScanConflictMarkerTrivia();
                            
                            result.AddMessages(messages);
                            
                            // if (_skipTrivia)
                            // {
                            //     continue;
                            // }
                            // else
                            // {
                                token.Kind = SyntaxKind.ConflictMarkerTrivia;
                                
                                return result;
                            // }
                        }
                        Pos++;
                        token.Kind = SyntaxKind.GreaterThanToken;
                        
                        

                        return result;
                    }
                    case (int)CharacterCodes.Question:{
                        Pos++;
                        token.Kind = SyntaxKind.QuestionToken;
                        

                        return result;
                    }

                    case (int)CharacterCodes.OpenBracket:{
                        Pos++;

                        token.Kind = SyntaxKind.OpenBracketToken;
                        
                        return result;
                    }

                    case (int)CharacterCodes.CloseBracket:{
                        Pos++;
                        token.Kind = SyntaxKind.CloseBracketToken;
                        

                        return result;
                    }

                    case (int)CharacterCodes.Caret:{
                        if (S1.CharCodeAt(SourceText, Pos + 1) == (int)CharacterCodes.Equals)
                        {
                            Pos += 2;
                            token.Kind = SyntaxKind.CaretEqualsToken;
                            

                            return result;
                        }
                        Pos++;
                        token.Kind = SyntaxKind.CaretToken;
                        

                        return result;
                    }

                    case (int)CharacterCodes.OpenBrace:{
                        Pos++;
                        token.Kind = SyntaxKind.OpenBraceToken;
                        

                        return result;
                    }
                    case (int)CharacterCodes.Bar:{
                        if (S1.CharCodeAt(SourceText, Pos + 1) == (int)CharacterCodes.Bar)
                        {
                            Pos += 2;
                            token.Kind = SyntaxKind.BarBarToken;
                            
                            

                            return result;
                        }
                        if (S1.CharCodeAt(SourceText, Pos + 1) == (int)CharacterCodes.Equals)
                        {
                            Pos += 2;
                            token.Kind = SyntaxKind.BarEqualsToken;
                            
                            

                            return result;
                        }
                        Pos++;
                        token.Kind = SyntaxKind.BarToken;
                        
                        

                        return result;
                    }

                    case (int)CharacterCodes.CloseBrace:{
                        Pos++;
                        token.Kind = SyntaxKind.CloseBraceToken;
                        
                        

                        return result;
                    }

                    case (int)CharacterCodes.Tilde:{
                        Pos++;
                        token.Kind = SyntaxKind.TildeToken;
                        
                        

                        return result;
                    }
                    
                    case (int)CharacterCodes.At:{
                        Pos++;
                        token.Kind = SyntaxKind.AtToken;
                        

                        return result;
                    }

                    case (int)CharacterCodes.Backslash:{
                        var cookedChar = PeekUnicodeEscape();
                        if (cookedChar >= 0 && S1.IsIdentifierStart(cookedChar/*, _languageVersion*/))
                        {
                            Pos += 6;
                            var lexeme = S1.StringFromCharCode(cookedChar) + ScanIdentifierParts();
                            
                            token.Kind = S1.GetIdentifierToken(lexeme, TokenMap);
                            token.Lexeme = lexeme;

                            return result;
                        }

                        result.AddMessages(
                            new ParsingMessage(MessageKind.Error, $"Invalid character '{cookedChar}'", new Range(this.Pos, this.Pos))
                        );
                        
                        Pos++;
                        token.Kind = SyntaxKind.Unknown;
                        

                        return result;
                    }

                    default:{
                        if (S1.IsIdentifierStart(ch/*, _languageVersion*/))
                        {
                            Pos++;
                            while (Pos < end && S1.IsIdentifierPart(ch = S1.CharCodeAt(SourceText, Pos)/*, _languageVersion*/))
                            { 
                                Pos++;
                            }
                            
                            var lexeme = SourceText.Substring(token.StartPos, Pos - token.StartPos);
                            
                            if (ch == (int)CharacterCodes.Backslash)
                            {
                                lexeme += ScanIdentifierParts();
                            }

                            token.Kind = S1.GetIdentifierToken(lexeme, TokenMap);
                            token.Lexeme = lexeme;

                            return result;
                        }
                        else if (S1.IsWhiteSpaceSingleLine(ch))
                        {
                            Pos++;
                            continue;
                        }
                        else if (S1.IsLineBreak(ch))
                        {
                            token.PrecedingLineBreak = true;
                            Pos++;
                            continue;
                        }

                        result.AddMessages(
                            new ParsingMessage(MessageKind.Error, $"Invalid character '{ch}'", new Range(this.Pos, this.Pos))
                        );

                        Pos++;
                        token.Kind = SyntaxKind.Unknown;
                        
                        

                        return result;
                    }
                }
            }
        }
    
    
        private Result<string> ScanString(bool allowEscapes = true)
        {
            var result = new Result<string>();
           
            var end = SourceText.Length;
            var quote = S1.CharCodeAt(SourceText, Pos);

            Pos++;
            
            var start = Pos;
            
            var lexeme = "";

            while (true)
            {
                if (Pos >= end)
                {
                    lexeme += SourceText.Substring(start, Pos - start);
                    // _tokenIsUnterminated = true;

                    result.AddMessages(
                        new ParsingMessage(MessageKind.Error, "Unterminated string literal", new Range(Pos, Pos))
                    );

                    break;
                }
                var ch = S1.CharCodeAt(SourceText, Pos);
                if (ch == quote)
                {
                    lexeme += SourceText.Substring(start, Pos - start);
                    Pos++;
                    break;
                }
                if (ch == (int)CharacterCodes.Backslash && allowEscapes)
                {
                    lexeme += SourceText.Substring(start, Pos - start);
                    
                    var (messages, l) = ScanEscapeSequence();
                    
                    result.AddMessages(messages);

                    lexeme += l;
                    
                    start = Pos;

                    continue;
                }
                if (S1.IsLineBreak(ch))
                {
                    lexeme += SourceText.Substring(start, Pos - start);

                    // _tokenIsUnterminated = true;
                    
                    result.AddMessages(
                        new ParsingMessage(MessageKind.Error, "Unterminated string literal", new Range(Pos, Pos))
                    );

                    break;
                }

                Pos++;
            }

            if(!HasErrors(result))
            {
                result.Value = lexeme;
            }

            return result;
        }

        public Result<XToken> ScanTemplateAndSetTokenValue()
        {
            var result = new Result<XToken>();

            var token = new XToken();
            
            var lexeme = "";

            var end = SourceText.Length;

            var startedWithBacktick = S1.CharCodeAt(SourceText, Pos) == (int)CharacterCodes.Backtick;
            
            Pos++;
            
            var start = Pos;

            while (true)
            {
                if (Pos >= end)
                {
                    lexeme += SourceText.Substring(start, Pos - start);
                    
                    // _tokenIsUnterminated = true;
                    
                    result.AddMessages(
                        new ParsingMessage(MessageKind.Error, "Unterminated template literal", new Range(Pos, Pos))
                    );

                    token.Kind = startedWithBacktick ? SyntaxKind.NoSubstitutionTemplateLiteral : SyntaxKind.TemplateTail;
                    
                    break;
                }
                
                var currChar = S1.CharCodeAt(SourceText, Pos);
                
                if (currChar == (int)CharacterCodes.Backtick)
                {
                    lexeme += SourceText.Substring(start, Pos - start);
                    
                    Pos++;
                    
                    token.Kind = startedWithBacktick ? SyntaxKind.NoSubstitutionTemplateLiteral : SyntaxKind.TemplateTail;
                    
                    break;
                }
                if (currChar == (int)CharacterCodes.Dollar && Pos + 1 < end && S1.CharCodeAt(SourceText, Pos + 1) == (int)CharacterCodes.OpenBrace)
                {
                    lexeme += SourceText.Substring(start, Pos - start);

                    Pos += 2;

                    
                    token.Kind = startedWithBacktick ? SyntaxKind.TemplateHead : SyntaxKind.TemplateMiddle;
                    
                    break;
                }
                if (currChar == (int)CharacterCodes.Backslash)
                {
                    lexeme += SourceText.Substring(start, Pos - start);
                    
                    var (messages, l) = ScanEscapeSequence();
                    
                    result.AddMessages(messages);

                    lexeme += l;

                    start = Pos;

                    continue;
                }
                if (currChar == (int)CharacterCodes.CarriageReturn)
                {
                    lexeme += SourceText.Substring(start, Pos - start);

                    Pos++;
                    
                    if (Pos < end && S1.CharCodeAt(SourceText, Pos) == (int)CharacterCodes.LineFeed)
                    {
                        Pos++;
                    }
                    
                    lexeme += "\n";
                    
                    start = Pos;
                    
                    continue;
                }
                Pos++;
            }
            //Debug.assert(resultingToken != null);

            token.Lexeme = lexeme;

            if(!HasErrors(result))
            {
                result.Value = token;
            }
           
            return result;
        }


        private Result<string> ScanEscapeSequence()
        {
            var result = new Result<string>();

            var end = SourceText.Length;

            Pos++;

            if (Pos >= end)
            {
                result.AddMessages(
                    new ParsingMessage(MessageKind.Error, "Unexpected end of text", new Range(Pos, Pos))
                );
    
                result.Value = "";

                return result;
            }

            var ch = S1.CharCodeAt(SourceText, Pos);
            
            Pos++;
            
            switch (ch)
            {
                case (int)CharacterCodes._0:{
                    result.Value = "\0";
                    return result;
                }

                case (int)CharacterCodes.b:{
                    result.Value = "\b";
                    return result;
                }

                case (int)CharacterCodes.t:{
                    result.Value = "\t";
                    return result;
                }

                case (int)CharacterCodes.n:{
                    result.Value = "\n";
                    return result;
                }

                case (int)CharacterCodes.v:{
                    result.Value = "\v";
                    return result;
                }

                case (int)CharacterCodes.f:{
                    result.Value = "\f";
                    return result;
                }

                case (int)CharacterCodes.r:{
                    result.Value = "\r";
                    return result;
                }

                case (int)CharacterCodes.SingleQuote:{
                    result.Value = "\'";
                    return result;
                }

                case (int)CharacterCodes.DoubleQuote:{
                    result.Value = "\"";
                    return result;
                }

                case (int)CharacterCodes.u:{
                    if (Pos < end && S1.CharCodeAt(SourceText, Pos) == (int)CharacterCodes.OpenBrace)
                    {
                        // _hasExtendedUnicodeEscape = true;
                        Pos++;

                        result.Value = ScanExtendedUnicodeEscape();

                        return result;
                    }

                    // '\uDDDD'
                    result.Value = ScanHexadecimalEscape(/*numDigits*/ 4);

                    return result;
                }

                case (int)CharacterCodes.x:{
                    // '\xDD'
                    result.Value = ScanHexadecimalEscape(/*numDigits*/ 2);
                    return result;
                }


                case (int)CharacterCodes.CarriageReturn:
                    if (Pos < end && S1.CharCodeAt(SourceText, Pos) == (int)CharacterCodes.LineFeed)
                    {
                        Pos++;
                    }
                    goto caseLabel15;

                case (int)CharacterCodes.LineFeed:
                case (int)CharacterCodes.LineSeparator:
                case (int)CharacterCodes.ParagraphSeparator:
                    caseLabel15:{
                        result.Value = "";
                        return result;
                }

                default:{
                    result.Value = ((char)ch).ToString();
                    return result;
                }
            }
        }

        private Result<object> ScanConflictMarkerTrivia()
        {
            var result = new Result<object>();

            // result.AddMessages(
            //     new ParsingMessage(MessageKind.Error, "Merge conflict marker encountered", new Range(Pos, Pos))
            // );
// var s = Pos;
            var ch = S1.CharCodeAt(SourceText, Pos);
            var len = SourceText.Length;
            if (ch == (int)CharacterCodes.LessThan || ch == (int)CharacterCodes.GreaterThan)
            {
                while (Pos < len && !S1.IsLineBreak(S1.CharCodeAt(SourceText, Pos)))
                {
                    Pos++;
                }
            }
            else
            {
                ////Debug.assert(ch ==  (int)CharacterCodes.equals);
                while (Pos < len)
                {
                    var ch2 = S1.CharCodeAt(SourceText, Pos);
                    if (ch2 == (int)CharacterCodes.GreaterThan && S1.IsConflictMarkerTrivia(SourceText, Pos))
                    {
                        break;
                    }
                    Pos++;
                }
            }

            // result.Value = Pos;

            return result;
        }

        public Result<Lexer.XToken> ScanJSXToken()
        {
            var token = new XToken();

            var result = new Result<Lexer.XToken>{ Value = token };

            var start = token.StartPos = Pos;
            var end = SourceText.Length;

            if(Pos >= end)
            {
                token.Kind = SyntaxKind.EndOfFileToken;
                return result;
            }

            var @char = S1.CharCodeAt(SourceText, Pos);

            if (@char == (int)CharacterCodes.LessThan)
            {
                if (S1.CharCodeAt(SourceText, Pos + 1) == (int)CharacterCodes.Slash)
                {
                    Pos += 2;
                    token.Kind = SyntaxKind.LessThanSlashToken;
                    return result;
                }

                Pos++;
                token.Kind = SyntaxKind.LessThanToken;
                return result;
            }
            if (@char == (int)CharacterCodes.OpenBrace)
            {
                Pos++;
                token.Kind = SyntaxKind.OpenBraceToken;
                return result;
            }
            while (Pos < end)
            {
                Pos++;
                @char = S1.CharCodeAt(SourceText, Pos);

                if (@char == (int)CharacterCodes.OpenBrace)
                {
                    break;
                }
                if (@char == (int)CharacterCodes.LessThan)
                {
                    if (S1.IsConflictMarkerTrivia(SourceText, Pos))
                    {
                        result.AddMessages(ScanConflictMarkerTrivia());
                        token.Kind = SyntaxKind.ConflictMarkerTrivia;
                        return result;
                    }
                    break;
                }
            }
            token.Kind = SyntaxKind.JsxText;
            return result;
        }

        public Result<Lexer.XToken> ScanJSXIdentifier()
        {
            var token = new XToken();

            var result = new Result<Lexer.XToken>{ Value = token };

            var start = token.StartPos = Pos;
            var end = SourceText.Length;

            var firstCharPosition = Pos;

            while (Pos < end)
            {
                var ch = S1.CharCodeAt(SourceText, Pos);
                if (ch == (int)CharacterCodes.Minus || ((firstCharPosition == Pos) ? S1.IsIdentifierStart(ch) : S1.IsIdentifierPart(ch)))
                {
                    Pos++;
                }
                else
                {
                    break;
                }
            }
            
            var length = Pos - start;

            if(length == 0)
            {
                result.AddMessages(
                    new ParsingMessage(MessageKind.Error, "JSX identifier expected", new Range(start, Pos))
                );
            }
            else
            {
                token.Lexeme += SourceText.Substring(firstCharPosition, Pos - firstCharPosition);
                token.Kind = SyntaxKind.Identifier;

                result.Value = token;
            }


            return result;
        }

        public Result<Lexer.XToken> ScanJSXAttributeValue()
        {
            var result = new Result<Lexer.XToken>();

            var start = Pos;

            switch (S1.CharCodeAt(SourceText, Pos))
            {
                case (int)CharacterCodes.DoubleQuote:
                case (int)CharacterCodes.SingleQuote:
                {
                    var token = new XToken();
                    token.Lexeme = result.AddMessages(ScanString(/*allowEscapes*/ false));
                    token.Kind = SyntaxKind.StringLiteral;

                    result.Value = token;
                }
                break;

                default:{
                    // If this scans anything other than `{`, it's a parse error.
                    var next = result.AddMessages(NextToken());

                    if(next.Kind == SyntaxKind.OpenBraceToken)
                    {
                        result.Value = next;
                    }
                    else
                    {
                        result.AddMessages(
                            new ParsingMessage(MessageKind.Error, "JSX attribute value expected", new Range(start, Pos))
                        );
                    }
                }
                break;
            }
        
            return result;
        }

        private string ScanIdentifierParts()
        {
            var result = "";

            var start = Pos;

            var end = SourceText.Length;

            while (Pos < end)
            {
                var ch = S1.CharCodeAt(SourceText, Pos);

                if (S1.IsIdentifierPart(ch/*, _languageVersion*/))
                {
                    Pos++;
                }
                else
                if (ch == (int)CharacterCodes.Backslash)
                {
                    ch = PeekUnicodeEscape();
                    if (!(ch >= 0 && S1.IsIdentifierPart(ch/*, _languageVersion*/)))
                    {
                        break;
                    }
                    result += SourceText.Substring(start, Pos - start);
                    result += S1.StringFromCharCode(ch);
                    // Valid Unicode escape is always six characters
                    Pos += 6;
                    start = Pos;
                }
                else
                {
                    break;
                }
            }
            result += SourceText.Substring(start, Pos - start);
            return result;
        }


        private string ScanHexadecimalEscape(int numDigits)
        {
            var result = new Result<string>();

            var escapedValue = ScanExactNumberOfHexDigits(numDigits);
            if (escapedValue >= 0)
            {
                return S1.StringFromCharCode(escapedValue);
            }
            else
            {
                result.AddMessages(
                    new ParsingMessage(MessageKind.Error, "Hexadecimal digit expected", new Range(Pos, Pos))
                );

                return "";
            }
        }

        private int ScanBinaryOrOctalDigits(int radix)
        {
            var value = 0;
            var numberOfDigits = 0;
            
            while (true)
            {
                var ch = S1.CharCodeAt(SourceText, Pos);
                var valueOfCh = ch - (int)CharacterCodes._0;
                if (!S1.IsDigit(ch) || valueOfCh >= radix)
                {
                    break;
                }
                value = value * radix + valueOfCh;
                Pos++;
                numberOfDigits++;
            }

            if (numberOfDigits == 0)
            {
                value = -1;
            }
            
            return value;
        }


        private string ScanExtendedUnicodeEscape()
        {
            var result = new Result<string>();

            var escapedValue = ScanMinimumNumberOfHexDigits(1);
         
            var isInvalidExtendedEscape = false;
         
            var end = SourceText.Length;

            if (escapedValue < 0)
            {
                result.AddMessages(
                    new ParsingMessage(MessageKind.Error, "Hexadecimal digit expected", new Range(Pos, Pos))
                );

                isInvalidExtendedEscape = true;
            }
            else
            if (escapedValue > 0x10FFFF)
            {
                result.AddMessages(
                    new ParsingMessage(MessageKind.Error, "An extended Unicode escape value must be between 0x0 and 0x10FFFF inclusive", new Range(Pos, Pos))
                );

                isInvalidExtendedEscape = true;
            }
            if (Pos >= end)
            {
                
                result.AddMessages(
                    new ParsingMessage(MessageKind.Error, "Unexpected end of text", new Range(Pos, Pos))
                );

                isInvalidExtendedEscape = true;
            }
            else
            if (S1.CharCodeAt(SourceText, Pos) == (int)CharacterCodes.CloseBrace)
            {
                // Only swallow the following character up if it's a '}'.
                Pos++;
            }
            else
            {
                result.AddMessages(
                    new ParsingMessage(MessageKind.Error, "Unterminated Unicode escape sequence", new Range(Pos, Pos))
                );

                isInvalidExtendedEscape = true;
            }
            if (isInvalidExtendedEscape)
            {
                return "";
            }
            return Utf16EncodeAsString(escapedValue);
        }

        private Result<string> ScanNumber()
        {
            var result = new Result<string>();

            var start = Pos;
            while (S1.IsDigit(S1.CharCodeAt(SourceText, Pos)))
            {
                Pos++;
            }
            if (S1.CharCodeAt(SourceText, Pos) == (int)CharacterCodes.Dot)
            {
                Pos++;
                while (S1.IsDigit(S1.CharCodeAt(SourceText, Pos)))
                {
                    Pos++;
                }
            }
            var end = Pos;
            if (S1.CharCodeAt(SourceText, Pos) == (int)CharacterCodes.E || S1.CharCodeAt(SourceText, Pos) == (int)CharacterCodes.e)
            {
                Pos++;
                if (S1.CharCodeAt(SourceText, Pos) == (int)CharacterCodes.Plus || S1.CharCodeAt(SourceText, Pos) == (int)CharacterCodes.Minus)
                {
                    Pos++;
                }
                if (S1.IsDigit(S1.CharCodeAt(SourceText, Pos)))
                {
                    Pos++;
                    while (S1.IsDigit(S1.CharCodeAt(SourceText, Pos)))
                    {
                        Pos++;
                    }
                    end = Pos;
                }
                else
                {
                    result.AddMessages(
                        new ParsingMessage(MessageKind.Error, "Digit expected", new Range(Pos, Pos))
                    );
                }
            }

            if(!HasErrors(result))
            {
                result.Value = SourceText.Substring(start, end - start);
            }

            return result;
        }


        private int ScanOctalDigits()
        {
            var start = Pos;
            
            while (S1.IsOctalDigit(S1.CharCodeAt(SourceText, Pos)))
            {
                Pos++;
            }

            return int.Parse(SourceText.Substring(start, Pos - start));
        }

        private int ScanExactNumberOfHexDigits(int count = 1)
        {
            return ScanHexDigits(/*minCount*/ count, /*scanAsManyAsPossible*/ false);
        }


        private int ScanMinimumNumberOfHexDigits(int count = 1)
        {
            return ScanHexDigits(/*minCount*/ count, /*scanAsManyAsPossible*/ true);
        }

        private string ScanDirective()
        {
            var directive = S1.DirectiveRegex.Match(SourceText.Substring(Pos)).Captures[0].Value;

            Pos += directive.Length;

            return directive;
        }

        private string ScanShebangTrivia()
        {
            var shebang = S1.ShebangTriviaRegex.Match(SourceText).Captures[0].Value;

            Pos += shebang.Length;

            return shebang;
        }

        private int ScanHexDigits(int minCount = 1, bool scanAsManyAsPossible = true)
        {
            var digits = 0;
            var value = 0;
            while (digits < minCount || scanAsManyAsPossible)
            {
                var ch = S1.CharCodeAt(SourceText, Pos);
                if (ch >= (int)CharacterCodes._0 && ch <= (int)CharacterCodes._9)
                {
                    value = value * 16 + ch - (int)CharacterCodes._0;
                }
                else
            if (ch >= (int)CharacterCodes.A && ch <= (int)CharacterCodes.F)
                {
                    value = value * 16 + ch - (int)CharacterCodes.A + 10;
                }
                else
            if (ch >= (int)CharacterCodes.a && ch <= (int)CharacterCodes.f)
                {
                    value = value * 16 + ch - (int)CharacterCodes.a + 10;
                }
                else
                {
                    break;
                }
                Pos++;
                digits++;
            }
            if (digits < minCount)
            {
                value = -1;
            }
            return value;
        }

        private string Utf16EncodeAsString(int codePoint)
        {
            Debug.Assert(0x0 <= codePoint && codePoint <= 0x10FFFF);
            if (codePoint <= 65535)
            {
                return S1.StringFromCharCode(codePoint);
            }
            var codeUnit1 = (int)Math.Floor(((double)codePoint - 65536) / 1024) + 0xD800;
            var codeUnit2 = ((codePoint - 65536) % 1024) + 0xDC00;
            return S1.StringFromCharCode(codeUnit1, codeUnit2);
        }

        private int PeekUnicodeEscape()
        {
            var end = SourceText.Length;

            if (Pos + 5 < end && S1.CharCodeAt(SourceText, Pos + 1) == (int)CharacterCodes.u)
            {
                var start = Pos;
                Pos += 2;
                var value = ScanExactNumberOfHexDigits(4);
                Pos = start;
                return value;
            }
            return -1;
        }

    }
}