using System;
using System.Text;
using UnityEngine;

namespace GameplayTags.Editor
{
    public class GameplayTagEditorUtilities
    {
        public static string FormatGameplayTagQueryDescriptionToLines(in string desc)
        {
            if (desc.StartsWith(" ALL(") || desc.StartsWith(" ANY(") || desc.StartsWith(" NONE("))
            {
                StringBuilder @string = new();

                static void outputIndent(StringBuilder sb, int indent)
                {
                    sb.Append("\n");
                    for (int i = 0; i < indent; i++)
                    {
                        sb.Append("    ");
                    }
                }

                int indent = 0;
                foreach (char c in desc)
                {
                    if (c == ' ')
                    {

                    }
                    else if (c == '(')
                    {
                        @string.Append(c);
                        indent++;
                        outputIndent(@string, indent);
                    }
                    else if (c == ')')
                    {
                        indent--;
                        outputIndent(@string, indent);
                        @string.Append(c);
                    }
                    else if (c == ',')
                    {
                        @string.Append(c);
                        outputIndent(@string, indent);
                    }
                    else
                    {
                        @string.Append(c);
                    }
                }

                return @string.ToString();
            }

            return desc;
        }
    }
}