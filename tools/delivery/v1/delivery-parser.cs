using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
// Strict grammar and duplicate-key guard, shared by Windows PowerShell 5.1 and 7.
public sealed class DbceDeliveryJsonV1 {
 readonly string s; int i; int depth;
 DbceDeliveryJsonV1(string text){s=text;}
 public static void Check(string text){var p=new DbceDeliveryJsonV1(text);p.Value();p.Space();if(p.i!=text.Length)throw new FormatException("Trailing JSON");}
 void Space(){while(i<s.Length && (s[i]==' '||s[i]=='\r'||s[i]=='\n'||s[i]=='\t'))i++;}
 void Need(char c){Space();if(i>=s.Length||s[i++]!=c)throw new FormatException("Expected "+c);}
 bool Take(char c){Space();if(i<s.Length&&s[i]==c){i++;return true;}return false;}
 string String(){Need('"');var b=new StringBuilder();while(i<s.Length){char c=s[i++];if(c=='"')return b.ToString();if(c<32)throw new FormatException("Control character");if(c!='\\'){b.Append(c);continue;}if(i>=s.Length)break;c=s[i++];switch(c){case '"':case '\\':case '/':b.Append(c);break;case 'b':b.Append('\b');break;case 'f':b.Append('\f');break;case 'n':b.Append('\n');break;case 'r':b.Append('\r');break;case 't':b.Append('\t');break;case 'u':if(i+4>s.Length)throw new FormatException("Unicode escape");b.Append((char)int.Parse(s.Substring(i,4),NumberStyles.HexNumber,CultureInfo.InvariantCulture));i+=4;break;default:throw new FormatException("Escape");}}throw new FormatException("Unclosed string");}
 void Value(){Space();if(++depth>64)throw new FormatException("JSON too deep");if(i>=s.Length)throw new FormatException("Missing value");char c=s[i];if(c=='{'){i++;var keys=new HashSet<string>(StringComparer.OrdinalIgnoreCase);if(!Take('}')){do{string key=String();if(!keys.Add(key))throw new FormatException("Duplicate JSON key: "+key);Need(':');Value();}while(Take(','));Need('}');}}else if(c=='['){i++;if(!Take(']')){do{Value();}while(Take(','));Need(']');}}else if(c=='"'){String();}else if(c=='t'){Literal("true");}else if(c=='f'){Literal("false");}else if(c=='n'){Literal("null");}else{if(Take('-')&&i>=s.Length)throw new FormatException("Number");if(i<s.Length&&s[i]=='0')i++;else{Digits(true);}if(i<s.Length&&s[i]=='.'){i++;Digits(true);}if(i<s.Length&&(s[i]=='e'||s[i]=='E')){i++;if(i<s.Length&&(s[i]=='+'||s[i]=='-'))i++;Digits(true);}}depth--;}
 void Digits(bool required){int start=i;while(i<s.Length&&s[i]>='0'&&s[i]<='9')i++;if(required&&i==start)throw new FormatException("Number");}
 void Literal(string v){if(i+v.Length>s.Length||s.Substring(i,v.Length)!=v)throw new FormatException("Literal");i+=v.Length;}
}
