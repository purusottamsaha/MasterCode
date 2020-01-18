using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GetWorkerSample
{
    class LineInvoiceModelDTO
    {
        public decimal Line_Extended_Amount { get; set; }
        public decimal Line_Qty { get; set; }
        public decimal Line_Unit_Cost { get; set; }
        public string Line_Cost_Center { get; set; }


        public string Line_Spend_Category_ID { get; set; }

        public string Line_Employee_ID { get; set; }
        public string Line_Project_ID { get; set; }

        public string Line_Organization_Reference_ID { get; set; }

        public string Line_Custom_Organization_Reference_ID { get; set; }

        public string Line_PO_Number { get; set; }

        public string Line_Item_Description { get; set; }




    }
}
