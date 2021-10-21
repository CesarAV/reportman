﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Reflection;


namespace Reportman.Drawing
{
    public class ObjectViewModel : INotifyPropertyChanged
    {
        ReadOnlyCollection<ObjectViewModel> _children;
        readonly ObjectViewModel _parent;
        readonly object _object;
        readonly FieldInfo _info;
        readonly Type _type;

        bool _isExpanded;
        bool _isSelected;

        public ObjectViewModel(object obj)
            : this(obj, null, null)
        {
        }
        public object ObjectInstance
        {
            get
            {
                return _object;
            }
        }
        ObjectViewModel(object obj, FieldInfo info, ObjectViewModel parent)
        {
            _object = obj;
            _info = info;
            if (_object != null)
            {
                _type = obj.GetType();
                if (!IsPrintableType(_type))
                {
                    // load the _children object with an empty collection to allow the + expander to be shown
                    _children = new ReadOnlyCollection<ObjectViewModel>(new ObjectViewModel[] { new ObjectViewModel(null) });
                }
            }
            _parent = parent;
        }

        public void LoadChildren()
        {
            _children = new ReadOnlyCollection<ObjectViewModel>(new List<ObjectViewModel>());
            if (_object != null)
            {
                // exclude value types and strings from listing child members
                if (!IsPrintableType(_type))
                {
                    // the public properties of this object are its children
                    /*var children = _type.GetProperties()
                        .Where(p => !p.GetIndexParameters().Any()) // exclude indexed parameters for now
                        .Select(p => new ObjectViewModel(p.GetValue(_object, null), p, this))
                        .ToList()*/
                    List<ObjectViewModel> children = new List<ObjectViewModel>();
                    foreach (FieldInfo ninfo in _type.GetFields())
                    {
                        if ((ninfo.IsPublic)  && (!ninfo.IsStatic))
                            children.Add(new ObjectViewModel(ninfo.GetValue(_object),ninfo,this));
                    }
                    /*var children = _type.GetFields()
                        .Select(p => new ObjectViewModel(p.GetValue(_object), p, this))
                        .ToList();*/

                    

                    // if this is a collection type, add the contained items to the children

                    var collection = _object as IEnumerable;
                    if (collection != null)
                    {
                        foreach (var item in collection)
                        {
                            children.Add(new ObjectViewModel(item, null, this)); // todo: add something to view the index value
                        }
                    }

                    _children = new ReadOnlyCollection<ObjectViewModel>(children);
                    this.OnPropertyChanged("Children");
                }
            }
        }

        /// <summary>
        /// Gets a value indicating if the object graph can display this type without enumerating its children
        /// </summary>
        static bool IsPrintableType(Type type)
        {
            return type != null && (
                type.IsPrimitive ||
                type.IsAssignableFrom(typeof(string)) ||
                type.IsEnum || IsBasicType(type));
        }
        static bool IsBasicType(Type type)
        {
            switch (type.ToString())
            {
                case "System.Decimal":
                case "System.Int64":
                case "System.Int32":
                case "System.String":
                case "System.Double":
                case "System.Single":
                case "System.Int16":
                case "System.Char":
                case "System.Byte":
                case "System.DateTime":
                case "System.TimeSpan":
                    return true;
            }
            return false;
        }
        public ObjectViewModel Parent
        {
            get { return _parent; }
        }

        public FieldInfo Info
        {
            get { return _info; }
        }

        public ReadOnlyCollection<ObjectViewModel> Children
        {
            get { return _children; }
        }

        public string Type
        {
            get
            {
                var type = string.Empty;
                if (_object != null)
                {
                    type = string.Format("({0})", _type.Name);
                }
                else
                {
                    if (_info != null)
                    {
                        type = string.Format("({0})", _info.GetType().Name);
                    }
                }
                return type;
            }
        }

        public string Name
        {
            get
            {
                var name = string.Empty;
                if (_info != null)
                {
                    name = _info.Name;
                }
                return name;
            }
        }

        public string Value
        {
            get
            {
                var value = string.Empty;
                if (_object != null)
                {
                    if (IsPrintableType(_type))
                    {
                        value = _object.ToString();
                    }
                }
                else
                {
                    value = "<null>";
                }
                return value;
            }
        }

        #region Presentation Members

        public bool IsExpanded
        {
            get { return _isExpanded; }
            set
            {
                if (_isExpanded != value)
                {
                    _isExpanded = value;
                    if (_isExpanded)
                    {
                        LoadChildren();
                    }
                    this.OnPropertyChanged("IsExpanded");
                }

                // Expand all the way up to the root.
                if (_isExpanded && _parent != null)
                {
                    _parent.IsExpanded = true;
                }
            }
        }

        public bool IsSelected
        {
            get { return _isSelected; }
            set
            {
                if (_isSelected != value)
                {
                    _isSelected = value;
                    this.OnPropertyChanged("IsSelected");
                }
            }
        }

        public bool NameContains(string text)
        {
            if (String.IsNullOrEmpty(text) || String.IsNullOrEmpty(Name))
            {
                return false;
            }

            return Name.IndexOf(text, StringComparison.InvariantCultureIgnoreCase) > -1;
        }

        public bool ValueContains(string text)
        {
            if (String.IsNullOrEmpty(text) || String.IsNullOrEmpty(Value))
            {
                return false;
            }

            return Value.IndexOf(text, StringComparison.InvariantCultureIgnoreCase) > -1;
        }

        #endregion

        #region INotifyPropertyChanged Members

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            if (this.PropertyChanged != null)
            {
                this.PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        #endregion
    }
    public class ObjectViewModelHierarchy
    {
        readonly ReadOnlyCollection<ObjectViewModel> _firstGeneration;
        readonly ObjectViewModel _rootObject;

        public ObjectViewModelHierarchy(object rootObject)
        {
            _rootObject = new ObjectViewModel(rootObject);
            _firstGeneration = new ReadOnlyCollection<ObjectViewModel>(new ObjectViewModel[] { _rootObject });
        }

        public ReadOnlyCollection<ObjectViewModel> FirstGeneration
        {
            get { return _firstGeneration; }
        }
    }
}
