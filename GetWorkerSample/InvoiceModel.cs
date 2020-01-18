using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GetWorkerSample
{
    class InvoiceModel
    {
        public string OCR_Image_Name { get; set; }
        public string OCR_Invoice_Number { get; set; }
        public string Company_Id { get; set; }
        public string Line_no { get; set; }

        public DateTime OCR_Invoice_Date { get; set; }
        public string spend_catagories_id { get; set; }
        public string cost_centre_refrence_id { get; set; }
        public string Qty { get; set; }
        public string Unit_Cost { get; set; }
        public string Extended_Amount { get; set; }
        public string OCR_supplier_Name { get; set; }
        public string OCR_supplier_ID{ get; set; }
        public decimal OCR_Invoice_Amount { get; set; }
        // public string Extended_Amount { get; set; }
        public string Bill_To { get; set; }
        public string OCR_Sender_Email_ID { get; set; }
        public string OCR_Currency { get; set; }
        public string OCR_Remit_to_address { get; set; }
        public string OCR_Bill_to { get; set; }
        public string OCR_Payment_Term { get; set; }
        public string OCR_AccountNo { get; set; }
        public string OCR_Net_Amount { get; set; }
        public string OCR_Tax_Amount { get; set; }
        public string OCR_PO_Number { get; set; }
       public string OCR_Probable_POs { get; set; }
        public string OCR_Description { get; set; }

        

    }
}
