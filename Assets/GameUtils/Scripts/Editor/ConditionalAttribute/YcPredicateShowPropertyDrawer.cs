using UnityEngine;
using UnityEditor;
using NCalc;
using System.Text.RegularExpressions;

[CustomPropertyDrawer(typeof(YcPredicateShowAttribute))]
public class YcPredicateShowPropertyDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        YcPredicateShowAttribute condHAtt = (YcPredicateShowAttribute)this.attribute;
        bool enabled = this.GetResult(condHAtt, property);
        bool wasEnabled = GUI.enabled;
        GUI.enabled = enabled;
        if (enabled || condHAtt.IsReadOnly)
        {
            EditorGUI.PropertyField(position, property, label, true);
        }
        GUI.enabled = wasEnabled;
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        YcPredicateShowAttribute condHAtt = (YcPredicateShowAttribute)this.attribute;
        if (this.GetResult(condHAtt, property) || condHAtt.IsReadOnly)
        {
            return EditorGUI.GetPropertyHeight(property, label);
        }
        return -EditorGUIUtility.standardVerticalSpacing;
    }

    private bool GetResult(YcPredicateShowAttribute condHAtt, SerializedProperty property)
    {
        string replacedVars = ReplaceVariables(property, condHAtt.Predicate);

        var expr = new Expression(replacedVars);
        expr.EvaluateParameter += (name, args) =>
        {
            if (name == "null")
                args.Result = null;
        };
        return (bool)expr.Evaluate();
    }

    private object GetValueFromProperty(SerializedProperty property)
    {
        Object targetObject = property.serializedObject.targetObject;
        System.Type targetObjectClassType = targetObject.GetType();
        System.Reflection.FieldInfo field = targetObjectClassType.GetField(property.propertyPath);
        if (field != null)
        {
            return field.GetValue(targetObject);
        }
        return null;
    }

    private string ReplaceVariables(SerializedProperty property, string expression)
    {
        string substituted = Regex.Replace(expression, @"@(\w+)", match =>
        {
            string varName = match.Groups[1].Value;
            SerializedProperty newProperty = property.serializedObject.FindProperty(varName);
            object value = GetValueFromProperty(newProperty);

            if (value == null)
                return "null";
            if (value is string)
                return $"'{value}'";
            if (value is bool)
                return (bool)value ? "true" : "false";

            return value.ToString();
        });
        return substituted;
    }
}
