/*
 * Copyright (C) 2012 Kris Wong
 * Copyright (C) 2017 Alexis Ryan
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *      http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

// Based on code from https://github.com/wongk/MultiSelectSpinner

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Util;
using Android.Views;
using Android.Widget;

namespace Ao3TrackReader.Droid
{
    public class MultiSelectSpinner : Spinner, IDialogInterfaceOnMultiChoiceClickListener
    {
        String[] _items = null;
        bool[] _selection = null;

        ArrayAdapter<String> _proxyAdapter;


        public MultiSelectSpinner(Context context) :
            base(context)
        {
            Initialize();
        }

        public MultiSelectSpinner(Context context, IAttributeSet attrs) :
            base(context, attrs)
        {
            Initialize();
        }

        public MultiSelectSpinner(Context context, IAttributeSet attrs, int defStyle) :
            base(context, attrs, defStyle)
        {
            Initialize();
        }

        private void Initialize()
        {
            _proxyAdapter = new ArrayAdapter<String>(Context, Android.Resource.Layout.SimpleSpinnerItem);
            base.Adapter = _proxyAdapter;
        }

        void IDialogInterfaceOnMultiChoiceClickListener.OnClick(IDialogInterface dialog, int which, bool isChecked)
        {

            if (_selection != null && which < _selection.Length)
            {
                _selection[which] = isChecked;

                _proxyAdapter.Clear();
                _proxyAdapter.Add(BuildSelectedItemString());
                SetSelection(0);
            }
            else
            {

                throw new ArgumentException("Argument 'which' is out of bounds.");

            }
        }

        /**
         * {@inheritDoc}
         */
        override public bool PerformClick()
        {
            AlertDialog.Builder builder = new AlertDialog.Builder(Context);
            builder.SetMultiChoiceItems(_items, _selection, this);
            builder.Show();
            return true;
        }

        public override ISpinnerAdapter Adapter { get => base.Adapter; set => throw new ApplicationException("setAdapter is not supported by MultiSelectSpinner."); }

        /**
         * Sets the options for this spinner.
         * @param items
         */
        public void SetItems(IEnumerable<String> items)
        {
            _items = items.ToArray();
            _selection = new bool[_items.Length];

            Array.Fill(_selection, false);
        }

        /**
         * Sets the selected options based on an array of string.
         * @param selection
         */
        public void SetSelection(IEnumerable<String> selection)
        {
            foreach (String sel in selection)
            {
                for (int j = 0; j < _items.Length; ++j)
                {
                    if (_items[j] == sel)
                    {
                        _selection[j] = true;
                    }
                }
            }
        }

        /**
         * Sets the selected options based on an array of positions.
         * @param selectedIndicies
         */
        public void SetSelection(IEnumerable<int> selectedIndicies)
        {
            foreach (int index in selectedIndicies)
            {
                if (index >= 0 && index < _selection.Length)
                {
                    _selection[index] = true;
                }
                else
                {
                    throw new ArgumentException("Index " + index + " is out of bounds.");
                }
            }
        }

        /**
         * Returns a list of strings, one for each selected item.
         * @return
         */
        public IList<string> GetSelectedStrings()
        {
            IList<string> selection = new List<string>();
            for (int i = 0; i < _items.Length; ++i)
            {
                if (_selection[i])
                {
                    selection.Add(_items[i]);
                }
            }
            return selection;
        }

        /**
         * Returns a list of positions, one for each selected item.
         * @return
         */
        public IList<int> GetSelectedIndicies()
        {
            IList<int> selection = new List<int>();
            for (int i = 0; i < _items.Length; ++i)
            {
                if (_selection[i])
                {
                    selection.Add(i);
                }
            }
            return selection;
        }

        /**
         * Builds the string for display in the spinner.
         * @return comma-separated list of selected items
         */
        private String BuildSelectedItemString()
        {
            StringBuilder sb = new StringBuilder();
            bool foundOne = false;

            for (int i = 0; i < _items.Length; ++i)
            {
                if (_selection[i])
                {
                    if (foundOne)
                    {
                        sb.Append(", ");
                    }
                    foundOne = true;

                    sb.Append(_items[i]);
                }
            }

            return sb.ToString();
        }
    }
}