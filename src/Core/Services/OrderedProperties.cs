namespace AIStorm.Core.Services;

using System;
using System.Collections.Generic;
using System.Linq;

public class OrderedProperties
{
    private readonly List<KeyValuePair<string, string>> _properties = new();
    
    public OrderedProperties()
    {
    }
    
    public OrderedProperties(params (string key, string value)[] properties)
    {
        foreach (var (key, value) in properties)
        {
            Add(key, value);
        }
    }
    
    public void Add(string key, string value)
    {
        // Replace existing key if it exists
        for (int i = 0; i < _properties.Count; i++)
        {
            if (_properties[i].Key == key)
            {
                _properties[i] = new KeyValuePair<string, string>(key, value);
                return;
            }
        }
        
        // Add new key-value pair
        _properties.Add(new KeyValuePair<string, string>(key, value));
    }
    
    public bool TryGetValue(string key, out string value)
    {
        foreach (var prop in _properties)
        {
            if (prop.Key == key)
            {
                value = prop.Value;
                return true;
            }
        }
        
        value = string.Empty;
        return false;
    }
    
    public string this[string key]
    {
        get
        {
            if (TryGetValue(key, out var value))
                return value;
            throw new KeyNotFoundException($"Key '{key}' not found.");
        }
        set => Add(key, value);
    }
    
    public bool ContainsKey(string key) => _properties.Any(p => p.Key == key);
    
    public IEnumerable<KeyValuePair<string, string>> GetProperties() => _properties;
    
    public IEnumerable<string> Keys => _properties.Select(p => p.Key);
    
    public int Count => _properties.Count;
}
