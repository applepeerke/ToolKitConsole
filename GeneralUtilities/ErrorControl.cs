using System.Collections.Generic;
using System.Linq;
using System.Text;
using System;
using System.Data;
using System.Configuration;

namespace GeneralUtilities 
{
    public class ErrorControl
    {
        #region Definitions
        private static ErrorControl _errorControl;
        private static bool _isLogUtilSwitchedOn;
        private static DataTable errorMessage;
        private static DataTable warningMessage;
        private static List<string> textElements;
        private static int _errorThreshold = -1;
        private static bool _multiUser= true;

        private string _errorReason = "";
        const string Column_UserId = "userId";
        const string Column_Message = "Message";

        const string _EMPTY = ""; 

        #endregion
        #region Properties
        public static int ErrorThreshold
        {
            get { return _errorThreshold; }
            set { _errorThreshold = value; }
        }
        public string ErrorReason
        {
            get { return _errorReason; }
            set { _errorReason = value; }
        }
        public bool MultiUser
        {
            get { return _multiUser; }
            set { _multiUser = value; }
        }
        #endregion
        #region Constructor
        public static ErrorControl Instance()
        {
            if (_errorControl == null)
            {
                // The class reference
                _errorControl = new ErrorControl();

                // Configuration
                textElements = new List<string>();
				_isLogUtilSwitchedOn = LogUtil.Instance.GetSwitchedOn();

                // Data
                errorMessage = new DataTable();
                errorMessage.Columns.Add(Column_UserId, typeof(int));
                errorMessage.Columns.Add(Column_Message, typeof(string));

                warningMessage = new DataTable();
                warningMessage.Columns.Add(Column_UserId, typeof(int));
                warningMessage.Columns.Add(Column_Message, typeof(string));
            }
            return _errorControl;
        }
        #endregion
        #region Public methods
        #region EvaluateResult / AddError overloads
        public void AddWarning(int userId, string message, bool prefix = false)
        {

            if (!string.IsNullOrEmpty(message))
            {
                if (prefix) { message = "Warning: " + message; }
                warningMessage.Rows.Add(userId, message);
            }
        }
        public void AddError(int userId, string errorText)
        {
            if (!string.IsNullOrEmpty(errorText))
            {
                EvaluateResult(userId, ErrorThreshold, errorText, _EMPTY, true, 0, true, _EMPTY);
            }
        }
        public void AddError(int userId, string errorText, int key)
        {
            if (!string.IsNullOrEmpty(errorText))
            {
                EvaluateResult(userId, ErrorThreshold, errorText, _EMPTY, true, key, true, _EMPTY);
            }
        }
        public void AddError(int userId, string errorText = _EMPTY, string errorReason = _EMPTY, int key = 0, bool log = true, string tableName = _EMPTY)
        {
            if (!string.IsNullOrEmpty(errorText))
            {
                EvaluateResult(userId, ErrorThreshold, errorText, errorReason, true, key, log, tableName);
            }
        }
        public void AddError(int userId, Exception ex, string errorText = _EMPTY, int errorReason = 0, int key = 0, string tableName = _EMPTY)
        {
            errorText = errorText + (ex.InnerException != null ?  ex.InnerException.Message : ex.Message );
            bool DBtype = (errorReason == 1) ? true : false;
            EvaluateResult(userId, ErrorThreshold, errorText, _EMPTY, true, key, true, tableName, DBtype);
        }
        public void EvaluateResult(int userId, int result, string errorText = "", int errorReason = 0, int key = 0, string tableName = _EMPTY)
        {
            bool DBtype = (errorReason == 1) ? true : false;
            EvaluateResult(userId, result, errorText, _EMPTY, false, key, true, tableName, DBtype);
        }
        #endregion
        public string YieldError(int userId)
        {
            // output
            string returnMessage = string.Empty;
            string ERMessagefilter = string.Format("{0}={1}", Column_UserId, userId.ToString());

            if (HasErrorMessage(ERMessagefilter))
            // In case of errors, optionally add a completion message.
            {
                //if (_showCompletion && (HasRowsMutationResult(OKfilter) || HasRowsMutationResult(ERfilter)))
                //{
                //    CompletionMessage(userId, GetQuantity(OKfilter, Column_OKQuantity), GetQuantity(ERfilter, Column_ERQuantity));
                //}
            }
            if (HasErrorMessage(ERMessagefilter))
            {
                returnMessage = EncodeJsString(string.Join("\\n", GetErrorMessages(userId).ToArray()));
            }
            // Wrap up.
            InitializeErrorhandling(userId);
            return returnMessage;
        }

        public void InitializeErrorhandling(int userId)
        {
            DeleteErrorMessages(userId);
            DeleteWarningMessages(userId);
        }
        public string YieldWarning(int userId)
        {
            string returnMessage = string.Empty;
            if (WarningMessageCount(userId) > 0)
            {
                returnMessage = EncodeJsString(string.Join("\\n", GetWarningMessages(userId).ToArray()));
            }
            DeleteWarningMessages(userId);
            return returnMessage;
        }
        /// <summary>
        /// Gets the error message by identifier.
        /// </summary>
        /// <param name="userId">The user identifier.</param>
        /// <param name="id">The identifier.</param>
        /// <returns></returns>
        public string GetErrorMessageById(int userId, int id)
        {
            if (id != 0 && ErrorMessageCount(userId) > 0)
            {
                var errorMessages = GetErrorMessages(userId);
                string search = id.ToString();
                foreach (string m in errorMessages)
                {
                    if (m.Contains(search))
                    {
                        return m;
                    }
                }
            }
            return string.Empty;
        }

        // Get the Error messages for the user
        public List<string> GetErrorMessages(int userId)
        {
            List<string> messages = new List<string>();
            DataRow[] rows = errorMessage.Select(GetUserIdFilter(userId));
            foreach (DataRow row in rows)
            {
                messages.Add(row[Column_Message].ToString());
            }
            return messages;
        }

        // Get the Warning messages for the user
        public List<string> GetWarningMessages(int userId)
        {
            List<string> messages = new List<string>();
            DataRow[] rows = warningMessage.Select(GetUserIdFilter(userId));
            foreach (DataRow row in rows)
            {
                messages.Add(row[Column_Message].ToString());
            }
            return messages;
        }

        // Get Error count = MessageResult ERquantity +  ErrorMessage count
        public int GetErrorCount(int userId)
        {
            return errorMessage.Select(GetUserIdFilter(userId)).Count();
        }

        // Get Warnings count
        public int GetWarningCount(int userId)
        {
            return warningMessage.Select(GetUserIdFilter(userId)).Count();
        }
        #endregion Public methods
        #region Private methods
        private void CheckUserId(int userId)
        {
            if (MultiUser && userId <= 0)
            {
                throw new ArgumentOutOfRangeException("UserId", "The MultiUser property is true. Every method should pass a User Id > 0 to ErrorControl.");
            }
        }
        private void CompletionMessage(int userId, int rowsOK, int rowsER)
        {
            AddError(userId, String.Format("In total {0} id's are processed, {1} successfully, {2} failed.", (rowsOK + rowsER), rowsOK, rowsER), _EMPTY, 0, false);
        }
        private static string EncodeJsString(string s)
        {
            StringBuilder sb = new StringBuilder();
            foreach (char c in s)
            {
                switch (c)
                {
//                    case '"':
                    case '\'':
  //                      sb.Append("\\\"");
                        sb.Append("\"");
                        break;
                    default:
                        int i = (int)c;
                        if (i < 32 || i > 127)
                        {
                            // sb.AppendFormat("\\\\u{0:X04}", i);
                            sb.AppendFormat("\\u{0:X04}", i);

                        }
                        else
                        {
                            sb.Append(c);
                        }
                        break;
                }
            }
            return sb.ToString();
        }
        // AddError or EvaluateResult target.
        private void EvaluateResult(int userId, int result, string errorText, string errorReason, bool sureError, int key, bool log = true, string tableName = _EMPTY, bool DBerror = false)
        {
            // Error: combine text elements and return error message.
            if ((result < ErrorThreshold) || sureError)
            {
                // Construct message text
                if (!string.IsNullOrEmpty(errorReason))
                {
                    textElements.Add("Reason:");
                    if (errorReason[errorReason.Length - 1].ToString() == ".")
                    { textElements.Add(errorReason); }
                    else { textElements.Add(errorReason + "."); }
                }
                // Combine all text elements separated by a blank.
                string messageText = string.IsNullOrEmpty(errorText) ? string.Join(" ", textElements.ToArray()) : errorText + " " + string.Join(" ", textElements.ToArray());

                // Add errormessage
                if (!string.IsNullOrEmpty(messageText))
                {
                    errorMessage.Rows.Add(userId, messageText);

                    if (_isLogUtilSwitchedOn && log)
                    {
                        LogUtil.Instance.AddLine(messageText, "*ERROR", 0, _EMPTY, "ErrorControl", "EvaluateResult");
                    }
                }
            }
            textElements.Clear();
        }
        #region Private Static CRUD methods
 
        private static int ErrorMessageCount(int userId)
        {
            return errorMessage.Select(GetUserIdFilter(userId)).Length;
        }
        private static int WarningMessageCount(int userId)
        {
            return warningMessage.Select(GetUserIdFilter(userId)).Length;
        }
        private static bool HasErrorMessage(string filter)
        {
            return (errorMessage.Select(filter).FirstOrDefault() != null);
        }

        private static void DeleteErrorMessages(int userId)
        {
            var rows = errorMessage.Select(GetUserIdFilter(userId));
            DeleteRows(rows);
        }
        private static void DeleteWarningMessages(int userId)
        {
            var rows = warningMessage.Select(GetUserIdFilter(userId));
            DeleteRows(rows);
        }

        private static void DeleteRows(DataRow[] rows)
        {
            foreach (var row in rows)
            {
                // row can be null.
                if (row != null)
                {
                    row.Delete();
                }
            }
        }

        private static string GetUserIdFilter(int userId)
        {
            return string.Format("{0}={1}", Column_UserId, userId.ToString());
        }
        #endregion
        #endregion
    }
}
