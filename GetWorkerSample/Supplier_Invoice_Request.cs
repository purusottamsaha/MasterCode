using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.Xml;
using System.IO;
using System.Net;
using System.Xml.Linq;

namespace GetWorkerSample
{
    class InvoiceData
    {
        public string id { get; set; }
        public string value { get; set; }


    }
    class MyXmlData
    {
        public string MyXmlFile { get; set; }
        public string MyXmlNode { get; set; }
        public string MyXmlVal { get; set; }
    }

   static class Program
    {

        //   Invoice_Delivery_Log LogTable = new Invoice_Delivery_Log();
        static void Main(string[] args)
        {

            // GlobalEntity.Invoice_Delivery_Log.
            //   GlobalEntity.SaveChanges();
            //    GlobalEntity.Invoice_Delivery_Log.Add(new Invoice_Delivery_Log { });
            System.Net.ServicePointManager.SecurityProtocol = System.Net.SecurityProtocolType.Tls12;


            SecurityBindingElement sb = SecurityBindingElement.CreateUserNameOverTransportBindingElement();
           
            sb.IncludeTimestamp = false;
            const int lim = Int32.MaxValue;
            var timeout = TimeSpan.FromMinutes(5);

            var cb = new CustomBinding(
                sb,
                new TextMessageEncodingBindingElement(MessageVersion.Soap11, Encoding.UTF8)
                {
                    ReaderQuotas = new XmlDictionaryReaderQuotas
                    {
                        MaxDepth = lim,
                        MaxStringContentLength = lim,
                        MaxArrayLength = lim,
                        MaxBytesPerRead = lim,
                        MaxNameTableCharCount = lim
                    }
                },
                new HttpsTransportBindingElement
                {
                    MaxBufferPoolSize = lim,
                    MaxReceivedMessageSize = lim,
                    MaxBufferSize = lim,
                    Realm = string.Empty
                })
            {
                SendTimeout = timeout,
                ReceiveTimeout = timeout
            };


          Resource_ManagementPortClient RCM = new Resource_ManagementPortClient(cb, new EndpointAddress("https://wd2-impl-services1.workday.com/ccx/service/leggmason8/Resource_Management/v33.1"));

            //// Specify the username and password for
            RCM.ClientCredentials.UserName.UserName = "ISU_INT396_WAM_Supplier_Invoices_Inbound@leggmason8";
            RCM.ClientCredentials.UserName.Password = "9DOT44RUN384t";
            RCM.ClientCredentials.Windows.ClientCredential.UserName = "ISU_INT396_WAM_Supplier_Invoices_Inbound@leggmason8";
            RCM.ClientCredentials.Windows.ClientCredential.Password = "9DOT44RUN384t";
            RCM.ClientCredentials.Windows.AllowedImpersonationLevel = System.Security.Principal.TokenImpersonationLevel.Impersonation;
            RCM.ClientCredentials.UseIdentityConfiguration = true;
            RCM.ClientCredentials.Windows.AllowNtlm = true;
            basic_validation(RCM);

        }
        public static void basic_validation(Resource_ManagementPortClient RCM)
        {
            Dictionary<string, List<MyXmlData>> filesinfo = readPDF(@"C:\File_Repo\XML");

            Console.WriteLine("***Total FILE FOUND*** :" + filesinfo.Count);

            if (filesinfo.Count>0)
            {
                InvoiceModel readyinvoicemodel = null;
                int count = 0;
                foreach (var xmlitem in filesinfo)
                {
                  
                   readyinvoicemodel = mapto_invoice_model(xmlitem);
                   Console.WriteLine("Processing File  -:" + xmlitem.Key);
                    try
                    {
                        if (readyinvoicemodel.OCR_Image_Name == "")
                        {
                            Console.WriteLine("pdf file not located");
                        }
                        Process_Invoce_Request(RCM, readyinvoicemodel);
                    }
                    catch (Exception Ex)
                    {
                       // int count = 0


                        Console.WriteLine("STATUS:Failed To save to work day with File Name-:" + xmlitem.Key);
                        Console.WriteLine("***Exception***");
                        Console.WriteLine(Ex.Message);
                        Console.WriteLine("......................................................................................................................................................................");
                        continue;
                       // throw Ex; ;
                    }
                 


                }
            }
            else
            {
                Console.WriteLine("No file to read");
            }
        }

        public static void Process_Invoce_Request(Resource_ManagementPortClient RCM, InvoiceModel invoicemodel)
        {
            Submit_Supplier_Invoice_RequestType SSIR = new Submit_Supplier_Invoice_RequestType();
            //Console.WriteLine("Taking Dicession based on OCR_PO_Number");

            if (string.IsNullOrEmpty(invoicemodel.OCR_PO_Number))
            {
                Console.WriteLine("***NON_PO_INVOICE***");
                SSIR.Supplier_Invoice_Data = process_NON_PO_Invoice(invoicemodel);


            }
            else
            {
                Console.WriteLine("PO invoice,with OCR_PO_Number in XML:  "+invoicemodel.OCR_PO_Number);
                SSIR.Supplier_Invoice_Data = process_po_Invoice(invoicemodel);
                Console.WriteLine("Ready to Push to WorkDay");

            }
            SSIR.Business_Process_Parameters = new Financials_Business_Process_ParametersType() { Auto_Complete = false };
            SSIR.Add_Only = true;
          //  SSIR.version = "33.1";


            try
            {

                Log_To_Console(SSIR);

                var objres = RCM.Submit_Supplier_Invoice(SSIR);
                Console.WriteLine("Success with WorkDay ID :- " + objres.Supplier_Invoice_Reference.ID[0].Value);
            }
            catch (Exception ex)
            {

              throw ex;
            }



        }


           public static void Log_To_Console(Submit_Supplier_Invoice_RequestType SSIR) {


            WorkDay_Resourse_ManagementEntities1 GlobalEntity = new WorkDay_Resourse_ManagementEntities1();
            GlobalEntity.Invoice_Delivery_Log.Add(new Invoice_Delivery_Log
            {
                Supplier_Name = SSIR.Supplier_Invoice_Data.Suppliers_Invoice_Number,
                Status = "Failed",
                Sender_Email = "",
                Supplier_ID = "",
                Error_Detail = "",
                File_Name = "",
                Bill_To = "",
                Amount = 0,
                Processing_Time = DateTime.UtcNow,
            });

        //    GlobalEntity.add
          //  GlobalEntity.SaveChanges();

            Console.WriteLine("Suppliers_Invoice_Number:" + SSIR.Supplier_Invoice_Data.Suppliers_Invoice_Number);
            Console.WriteLine("Invoice_Date  :" + SSIR.Supplier_Invoice_Data.Invoice_Date);

            Console.WriteLine("Payment_Terms_Reference  :" + SSIR.Supplier_Invoice_Data.Payment_Terms_Reference.ID[0].Value);


            Console.WriteLine("Currency_Reference :" + SSIR.Supplier_Invoice_Data.Currency_Reference.ID[0].Value);

            Console.WriteLine("Company_Reference :" + SSIR.Supplier_Invoice_Data.Company_Reference.ID[0].Value);
            Console.WriteLine("Employee :" + SSIR.Supplier_Invoice_Data.Approver_Reference.ID[0].Value);


            Console.WriteLine("MEMO  :" + SSIR.Supplier_Invoice_Data.Memo);


            Console.WriteLine("Invoice recive date:  Db Credentail issue");

            Console.WriteLine("Due_Date_Override :" + SSIR.Supplier_Invoice_Data.Due_Date_Override);

            Console.WriteLine("Control_Amount_Total  :" + SSIR.Supplier_Invoice_Data.Control_Amount_Total);
            foreach (var item in SSIR.Supplier_Invoice_Data.Invoice_Line_Replacement_Data.Select((value, i) => new { i, value }))
            {
                Console.WriteLine("Line_NO:" + (item.i).ToString());
                Console.WriteLine("**************************************************************");

                Console.WriteLine("Extended Amount:" + item.value.Extended_Amount);
                Console.WriteLine("Spend_Catagories: " + item.value.Spend_Category_Reference.ID[0].Value);
                Console.WriteLine("Cost_Centre: " + item.value.Worktags_Reference[0].ID[0].Value);
                Console.WriteLine("Item_Description: " + item.value.Item_Description);
            }

        }
        public static Supplier_Invoice_DataType process_NON_PO_Invoice(InvoiceModel invoicemodel)
        {
          


            WorkDay_Resourse_ManagementEntities1 Entity = new WorkDay_Resourse_ManagementEntities1();

            Console.WriteLine("Seraching Data in NonPO_Line_Details Table for OCR_supplier_ID:  " + invoicemodel.OCR_supplier_ID); 


            List<NonPO_Line_Details> non_po_details = Entity.NonPO_Line_Details.Where(x => x.Supplier_ID.Equals(invoicemodel.OCR_supplier_ID)).ToList();

            List<Supplier_Invoice_Line_Replacement_DataType> Supplier_invoice_line_replacement_Line = new List<Supplier_Invoice_Line_Replacement_DataType>();
            Console.WriteLine("Record Count in NonPO_Line_Details Table :" + non_po_details.Count);
            Supplier_Invoice_DataType sisd = new Supplier_Invoice_DataType();
            if (non_po_details.Count>0)
            {
                Console.WriteLine("Creating Record with NonPO_Line_Details Table");

                Console.WriteLine("Creating "+ non_po_details.Count + "  Lines for OCR_supplier_ID " + invoicemodel.OCR_supplier_ID);

                foreach (NonPO_Line_Details item in non_po_details)
                {
                    

                    LineInvoiceModelDTO lineobjdata = new LineInvoiceModelDTO();
                    decimal getper= Decimal.Divide(Convert.ToDecimal(item.Percetage), 100);
                    lineobjdata.Line_Extended_Amount =  getper* invoicemodel.OCR_Invoice_Amount;
                    lineobjdata.Line_Spend_Category_ID = item.Spend_Catagories;
                    lineobjdata.Line_Cost_Center = item.Cost_Center;
                    lineobjdata.Line_Item_Description = item.Item_Desription;
                    lineobjdata.Line_Custom_Organization_Reference_ID = item.Custom_Org;
                    lineobjdata.Line_Employee_ID = item.Employee;
                    lineobjdata.Line_Project_ID = item.Project;

                    Console.WriteLine("XML Invoice Amount :" + invoicemodel.OCR_Invoice_Amount);
                    Console.WriteLine("% from DB :" + item.Percetage);
                    Console.WriteLine("Calulated Amount :" + lineobjdata.Line_Extended_Amount);

                    Console.WriteLine("------------------------------");

                    Supplier_invoice_line_replacement_Line = build_NON_PO_invoiceline(Supplier_invoice_line_replacement_Line, lineobjdata);
                }

            }
            else
            {
                var dta = Entity.Companies.ToList();

                Company Company = Entity.Companies.Where(x => x.Company_Name.Trim()==invoicemodel.OCR_Bill_to.Trim()).FirstOrDefault();

                Console.WriteLine("Creating Record with NonPO_Line_Details_Defult Table");

                Console.WriteLine("Bill To  from XML :"  + invoicemodel.OCR_Bill_to);
                if (Company != null)
                {
                    Console.WriteLine("Comapny ID from DB "+ Company.Company_ID);

                    NonPO_Line_Details_Default item = Entity.NonPO_Line_Details_Default.Where(x => x.Company_ID.Equals(Company.Company_ID)).FirstOrDefault();
                    LineInvoiceModelDTO lineobjdata = new LineInvoiceModelDTO();

                    if (item != null)
                    {
                        lineobjdata.Line_Extended_Amount = invoicemodel.OCR_Invoice_Amount;
                        lineobjdata.Line_Spend_Category_ID = item.Spend_Category;
                        lineobjdata.Line_Cost_Center = item.Cost_Center;
                        lineobjdata.Line_Custom_Organization_Reference_ID = item.Custom_Org;

                        Supplier_invoice_line_replacement_Line = build_NON_PO_invoiceline(Supplier_invoice_line_replacement_Line, lineobjdata);
                    }
                    else
                    {
                        Console.WriteLine("No record found in NonPO_Line_Details_Default for Company_ID:" + Company.Company_ID);

                    }
                   
                }
                else
                {

                    Console.WriteLine("No record  company record found  found");
                }

            }
         
            
            sisd= build_po_header(invoicemodel);
            sisd.Invoice_Line_Replacement_Data = Supplier_invoice_line_replacement_Line.ToArray();
            return sisd;
        }
        public static Supplier_Invoice_DataType process_po_Invoice(InvoiceModel XMLinvoicemodel)
        {
            Supplier_Invoice_DataType sisd = new Supplier_Invoice_DataType();
            WorkDay_Resourse_ManagementEntities1 Entity = new WorkDay_Resourse_ManagementEntities1();
           // PO_Header_Invoice headerinfo = Entity.PO_Header_Invoice.Where(x => x.Supplier_ID == XMLinvoicemodel.OCR_supplier_ID).FirstOrDefault();
            //////if (headerinfo != null)
           // {
                List<Supplier_Invoice_Line_Replacement_DataType> ReplacementLineobject = new List<Supplier_Invoice_Line_Replacement_DataType>();

          List<PO_Details> po_details = Entity.PO_Details.Where(x => x.Supllier_ID.Equals(XMLinvoicemodel.OCR_supplier_ID)).ToList();
            if (po_details.Count > 0)
            {


                decimal total_po_amount = po_details.Sum(x => x.Po_Amount) ?? 0;

                if (XMLinvoicemodel.OCR_Invoice_Amount > total_po_amount)
                {
                    foreach (PO_Details item in po_details)
                    {
                        LineInvoiceModelDTO lineobjdataDTO = new LineInvoiceModelDTO();
                        lineobjdataDTO.Line_Extended_Amount = Convert.ToDecimal(item.Po_Amount);
                        lineobjdataDTO.Line_Spend_Category_ID = null;
                        lineobjdataDTO.Line_Cost_Center = null;
                        lineobjdataDTO.Line_PO_Number = XMLinvoicemodel.OCR_PO_Number;
                        ReplacementLineobject = build_po_invoiceline(lineobjdataDTO, ReplacementLineobject);
                    }


                    PO_Details newitem = po_details[po_details.Count - 1];
                    decimal amountleft = XMLinvoicemodel.OCR_Invoice_Amount - total_po_amount;
                    LineInvoiceModelDTO lineobjdata = new LineInvoiceModelDTO();
                    lineobjdata.Line_Extended_Amount = amountleft;
                    lineobjdata.Line_Spend_Category_ID = newitem.Spend_Catagory;
                    lineobjdata.Line_Cost_Center = newitem.Cost_Centre;

                    ReplacementLineobject = build_po_invoiceline(lineobjdata, ReplacementLineobject);
                }
                else if (XMLinvoicemodel.OCR_Invoice_Amount < total_po_amount)
                {

                    PO_Details newitem = po_details.FirstOrDefault();
                    decimal amountleft = Decimal.Subtract(XMLinvoicemodel.OCR_Invoice_Amount, 0.01M);
                    LineInvoiceModelDTO lineobjdata = new LineInvoiceModelDTO();

                    lineobjdata.Line_Extended_Amount = amountleft;

                    lineobjdata.Line_PO_Number = XMLinvoicemodel.OCR_PO_Number;

                    ReplacementLineobject = build_po_invoiceline(lineobjdata, ReplacementLineobject);

                    PO_Details newlistdata = po_details.FirstOrDefault();
                    decimal newamountleft = 0.01M;
                    LineInvoiceModelDTO newlineobjdata = new LineInvoiceModelDTO();
                    newlineobjdata.Line_Extended_Amount = newamountleft;
                    newlineobjdata.Line_Spend_Category_ID = newlistdata.Spend_Catagory;
                    lineobjdata.Line_Cost_Center = newlistdata.Cost_Centre;


                    ReplacementLineobject = build_po_invoiceline(lineobjdata, ReplacementLineobject);

                }


                sisd = build_po_header(XMLinvoicemodel);
                sisd.Invoice_Line_Replacement_Data = ReplacementLineobject.ToArray();
            }
            else
            {
                List<Supplier_Invoice_Line_Replacement_DataType> Supplier_invoice_line_replacement_Line = new List<Supplier_Invoice_Line_Replacement_DataType>();
                var dta = Entity.Companies.ToList();

                Company Company = Entity.Companies.Where(x => x.Company_Name.Trim() == XMLinvoicemodel.OCR_Bill_to.Trim()).FirstOrDefault();

                if (Company != null)
                {

                    NonPO_Line_Details_Default item = Entity.NonPO_Line_Details_Default.Where(x => x.Company_ID.Equals(Company.Company_ID)).FirstOrDefault();
                    LineInvoiceModelDTO lineobjdata = new LineInvoiceModelDTO();
                    lineobjdata.Line_Extended_Amount = XMLinvoicemodel.OCR_Invoice_Amount;
                    lineobjdata.Line_Spend_Category_ID = item.Spend_Category;
                    lineobjdata.Line_PO_Number = XMLinvoicemodel.OCR_PO_Number;
                    lineobjdata.Line_Cost_Center = item.Cost_Center;
                    sisd = build_po_header(XMLinvoicemodel);
                    sisd.Invoice_Line_Replacement_Data = build_NON_PO_invoiceline(Supplier_invoice_line_replacement_Line, lineobjdata).ToArray();
                }
                else
                {

                    Console.WriteLine("No record  company record found  found");
                }
              
            }
            return sisd;
        }

        public static Supplier_Invoice_DataType build_po_header(InvoiceModel invoicemodel)
        {
            WorkDay_Resourse_ManagementEntities1 Entity = new WorkDay_Resourse_ManagementEntities1();

            Company Company = Entity.Companies.Where(x => x.Company_Name.Equals(invoicemodel.OCR_Bill_to)).FirstOrDefault();

           
         Supplier_Invoice_DataType headerinfo = new Supplier_Invoice_DataType();
            CompanyObjectIDType[] cid = new CompanyObjectIDType[]
         {
                new CompanyObjectIDType
               {

                type = "Company_Reference_ID", Value = Company.Company_ID

               }

         };
            CurrencyObjectIDType[] currencyid = new CurrencyObjectIDType[1]
            {
                new CurrencyObjectIDType
                {
                    type = "Currency_ID", Value = invoicemodel.OCR_Currency
                }
            };
            Payment_TermsObjectIDType[] paymentTerm = new Payment_TermsObjectIDType[1]
            {
                new Payment_TermsObjectIDType
                 {
                     type = "Payment_Terms_ID", Value = "NET30DAYS"
            }    };



           
            headerinfo.Supplier_Reference = new SupplierObjectType()
            {
                ID = new SupplierObjectIDType[]
            {
                new SupplierObjectIDType
            {
                type ="Supplier_ID",
                Value = invoicemodel.OCR_supplier_ID
            }

               }
            };

            headerinfo.Approver_Reference = new WorkerObjectType() { ID= new WorkerObjectIDType[] { new WorkerObjectIDType() {  type="Employee_ID" ,Value="30896" }  }  };

            string memo = "";

            if (String.IsNullOrEmpty(invoicemodel.OCR_PO_Number))
            {
                 memo = invoicemodel.OCR_Description;

            }
            else
            {
                 memo = invoicemodel.OCR_Description;

            }



             string path = @"C:\File_Repo\PDF\EM_US_20190406100035053.pdf";
           
          

            //  string url = @"\\prodwapa1\RPA\Output\Processed Files\Image";
            //
            List<string> Filesname = new List<string>();

             string[] filePaths = Directory.GetFiles(@"C:\File_Repo\PDF");
            //  filePaths.Contains(x => x.Equals());
            foreach (string item in filePaths)
            {
                string filename = Path.GetFileName(item);

                Filesname.Add(filename);
            }
            // string file = Filesname.Where(X => X.Equals((@"C:\File_Repo\PDF\" + invoicemodel.OCR_Image_Name));
             // string  foundfile = Filesname.Where(x => x.Contains(invoicemodel.OCR_Image_Name)).FirstOrDefault();
              string fls = filePaths.Where(x => x.Contains(@"C:\File_Repo\PDF\"+invoicemodel.OCR_Image_Name)).FirstOrDefault();



            //


            //  headerinfo.Document_Link = url;
            if (!string.IsNullOrEmpty(fls))
            {
                Byte[] bytes = File.ReadAllBytes(fls);
                float mb = (bytes.Length / 1024f) / 1024f;

                headerinfo.Attachment_Data = new Financials_Attachment_DataType[]
               {
                new Financials_Attachment_DataType {
                    File_Content = bytes,
                    Filename = Path.GetFileName(fls),
                    Comment = "PuruTesting" ,
                    Content_Type="pdf",
                    Encoding="base64",
                    Compressed=true,
                    CompressedSpecified=true
                }
           };
                Console.WriteLine("Found  PDF in local folder -: " + fls);
            }
            else
            {
                headerinfo.Attachment_Data = null;
                Console.WriteLine("No matching file found in local folder with Name: " + invoicemodel.OCR_Image_Name);
            }
           

            headerinfo.Suppliers_Invoice_Number = invoicemodel.OCR_Invoice_Number;
            headerinfo.Submit = true;
            headerinfo.Locked_in_Workday = true;
            headerinfo.Company_Reference = new CompanyObjectType() { ID = cid };
            headerinfo.Currency_Reference = new CurrencyObjectType() { ID = currencyid };
            headerinfo.Invoice_Date = invoicemodel.OCR_Invoice_Date;
            headerinfo.Due_Date_Override = DateTime.Now.AddDays(30);
            headerinfo.Payment_Terms_Reference = new Payment_TermsObjectType { ID = paymentTerm };

            headerinfo.Control_Amount_Total = invoicemodel.OCR_Invoice_Amount;
            headerinfo.Memo = memo;


         

            return headerinfo;
        }

        //Generate data for Non-PO Invoice
        public static List<Supplier_Invoice_Line_Replacement_DataType> build_NON_PO_invoiceline(List<Supplier_Invoice_Line_Replacement_DataType> Supplier_invoice_line_replacement_Line, LineInvoiceModelDTO InvoicemodelDTO)
        {
               List<Audited_Accounting_WorktagObjectType> listworktagobj = new List<Audited_Accounting_WorktagObjectType>();
           
                listworktagobj.Add(new Audited_Accounting_WorktagObjectType()
                {
                    ID = new List<Audited_Accounting_WorktagObjectIDType>()
                      {
                          new Audited_Accounting_WorktagObjectIDType
                          {
                                    type ="Cost_Center_Reference_ID",
                                    Value=InvoicemodelDTO.Line_Cost_Center
                          },
                          
                         
                      }.ToArray()
                });


            listworktagobj.Add(new Audited_Accounting_WorktagObjectType()
            {
                ID = new List<Audited_Accounting_WorktagObjectIDType>()
                      {
                          new Audited_Accounting_WorktagObjectIDType
            {
                type = "Custom_Organization_Reference_ID",
                Value = InvoicemodelDTO.Line_Custom_Organization_Reference_ID
            }


                      }.ToArray()
            });
            if (!string.IsNullOrEmpty(InvoicemodelDTO.Line_Employee_ID))
            {
                listworktagobj.Add(new Audited_Accounting_WorktagObjectType()
                {
                    ID = new List<Audited_Accounting_WorktagObjectIDType>()
                      {
                          new Audited_Accounting_WorktagObjectIDType
            {
                type = "Employee_ID",
                Value = InvoicemodelDTO.Line_Employee_ID
            }


                      }.ToArray()
                });
            }

            if(!string.IsNullOrEmpty(InvoicemodelDTO.Line_Project_ID))
            listworktagobj.Add(new Audited_Accounting_WorktagObjectType()
            {
                ID = new List<Audited_Accounting_WorktagObjectIDType>()
                      {
                          new Audited_Accounting_WorktagObjectIDType
            {
                type = "Project_ID",
                Value = InvoicemodelDTO.Line_Project_ID
            }


                      }.ToArray()
            });

           // projec

            Spend_CategoryObjectType catobjtype = new Spend_CategoryObjectType()
            {
                ID = new Spend_CategoryObjectIDType[]{ new Spend_CategoryObjectIDType {

                      type="Spend_Category_ID",
                      Value=InvoicemodelDTO.Line_Spend_Category_ID
                 } }
                };

            

              Supplier_invoice_line_replacement_Line.Add(new Supplier_Invoice_Line_Replacement_DataType
              {
                Extended_Amount = InvoicemodelDTO.Line_Extended_Amount,
                Spend_Category_Reference = catobjtype,
                Worktags_Reference = listworktagobj.ToArray(),
                Item_Description = InvoicemodelDTO.Line_Item_Description
                
                
              });
            
            return Supplier_invoice_line_replacement_Line;
        }

        public static List<Supplier_Invoice_Line_Replacement_DataType> build_po_invoiceline(LineInvoiceModelDTO lineobjdata , List<Supplier_Invoice_Line_Replacement_DataType> lineitem)
        {

            List<Audited_Accounting_WorktagObjectType> listworktagobj = new List<Audited_Accounting_WorktagObjectType>();
            Spend_CategoryObjectType catobjtype = new Spend_CategoryObjectType();
           // if(lineobjdata.Line_Cost_Center!=null)

            if (lineobjdata.Line_Cost_Center!=null)
            {
                listworktagobj.Add(new Audited_Accounting_WorktagObjectType()
                {
                    ID = new List<Audited_Accounting_WorktagObjectIDType>()
                      {
                          new Audited_Accounting_WorktagObjectIDType
                          {
                                    type ="Cost_Center_Reference_ID",
                                    Value=lineobjdata.Line_Cost_Center
                          }

                      }.ToArray()
                });

            }
            else
            {
                listworktagobj = null;
            }

            if (lineobjdata.Line_Spend_Category_ID!=null)
            {

              
                catobjtype.ID = new Spend_CategoryObjectIDType[]{ new Spend_CategoryObjectIDType {

                      type="Spend_Category_ID",

                      Value=lineobjdata.Line_Spend_Category_ID
                 } };


            }
            else
            {
                catobjtype = null;

            }
               

                Audited_Accounting_WorktagObjectType[] worktagobj2 =
                   new Audited_Accounting_WorktagObjectType[]{ new Audited_Accounting_WorktagObjectType {
                     ID= new Audited_Accounting_WorktagObjectIDType[]
                     {
                         new Audited_Accounting_WorktagObjectIDType {
                         type ="Cost_Center_Reference_ID",
                         Value=lineobjdata.Line_Cost_Center
                     },new Audited_Accounting_WorktagObjectIDType {
                         type ="Organization_Reference_ID",
                         Value="SIA-WAM Zaki Mohd"
                     } ,new Audited_Accounting_WorktagObjectIDType {
                         type ="Custom_Organization_Reference_ID",
                         Value="SIA-WAM Zaki Mohd"
                     }}

                 }};

            Purchase_Order_LineObjectType PurchaseObj = new Purchase_Order_LineObjectType()
            {
                ID = new Purchase_Order_LineObjectIDType[] {

                    new Purchase_Order_LineObjectIDType
                    {

                        parent_type ="Document_Number",
                        parent_id ="PO-0000019678",
                        Value="1",
                        type="Line_Number"

                }


            },
                Descriptor = "somedes"
            };


            lineitem.Add(new Supplier_Invoice_Line_Replacement_DataType
                {
                    Extended_Amount = lineobjdata.Line_Extended_Amount,
                    Quantity = 1,
                    Unit_Cost = lineobjdata.Line_Extended_Amount,
                    QuantitySpecified = true,
                    Unit_CostSpecified = true,
                    Spend_Category_Reference = null,
                    Purchase_Order_Line_Reference = PurchaseObj,
                    Worktags_Reference = null,
                    
                });

            

            return lineitem;
        }

        public static Dictionary<string, List<MyXmlData>> readPDF(string url)
        {
            string[] filePaths = Directory.GetFiles(url);
            List<MyXmlData> MyXmlList = new List<MyXmlData>();

            foreach (var item in filePaths)
            {

                //XDocument inputxml = XDocument.Load(item);

                //IEnumerable<XElement> childList =

                // from el in inputxml.Elements()

                //   select el;

                //foreach (XElement e in childList)

                //    Console.WriteLine(e);



                MyXmlData obj;
                XmlTextReader reader = new XmlTextReader(item);

                obj = new MyXmlData();

                while (reader.Read())

                {

                    obj.MyXmlFile = reader.BaseURI.Substring(reader.BaseURI.LastIndexOf("/") + 1);
                    switch (reader.NodeType)

                    {

                        case XmlNodeType.Element: // The node is an element.

                            //Console.Write("<" + reader.Name);

                            //Console.WriteLine(">");
                            obj.MyXmlNode = reader.Name;
                            break;

                        case XmlNodeType.Text: //Display the text in each element.

                            //Console.WriteLine(reader.Value);
                            obj.MyXmlVal = reader.Value;
                            break;

                        case XmlNodeType.EndElement: //Display the end of the element.

                            //Console.Write("</" + reader.Name);

                            //Console.WriteLine(">");
                            MyXmlList.Add(obj);
                            obj = new MyXmlData();
                            break;

                    }

                }



            }

            Dictionary<string, List<MyXmlData>> myDictionary = MyXmlList
           .GroupBy(o => o.MyXmlFile)
           .ToDictionary(g => g.Key, g => g.ToList());

            // grpfilename.Select(x => new InvoiceModel { currency=x.K });
            return myDictionary;
        }

        public static InvoiceModel mapto_invoice_model(KeyValuePair<string, List<MyXmlData>> xmlitem)
        {

            InvoiceModel modelinfo = null;
            modelinfo = new InvoiceModel();
            foreach (var item in xmlitem.Value)
            {
                switch (item.MyXmlNode)
                {
                    case "OCR_Image_Name":
                        modelinfo.OCR_Image_Name = item.MyXmlVal;
                        break;

                    case "OCR_PO_Number":
                        modelinfo.OCR_PO_Number = item.MyXmlVal;
                        break;

                    case "OCR_Invoice_Date":
                        modelinfo.OCR_Invoice_Date = Convert.ToDateTime(item.MyXmlVal);
                        break;
                    case "OCR_Invoice_Number":
                        modelinfo.OCR_Invoice_Number = item.MyXmlVal;
                        break;
                    case "OCR_supplier_Name":
                        modelinfo.OCR_supplier_Name = item.MyXmlVal;
                        break;
                    case "OCR_Supplier_ID":
                        modelinfo.OCR_supplier_ID = item.MyXmlVal;
                        break;
                    case "OCR_Sender_Email_ID":
                        modelinfo.OCR_Sender_Email_ID = item.MyXmlVal;
                        break;
                    case "OCR_Invoice_Amount":
                        modelinfo.OCR_Invoice_Amount = Convert.ToDecimal(item.MyXmlVal);
                        break;
                    case "OCR_Currency":
                        modelinfo.OCR_Currency = item.MyXmlVal;
                        break;
                    case "OCR_Remit_to_address":
                        modelinfo.OCR_Remit_to_address = item.MyXmlVal;
                        break;
                    case "OCR_Bill_to":
                        modelinfo.OCR_Bill_to = item.MyXmlVal;
                        break;
                    case "OCR_Payment_Term":
                        modelinfo.OCR_Payment_Term = item.MyXmlVal;
                        break;
                    case "OCR_AccountNo":
                        modelinfo.OCR_AccountNo = item.MyXmlVal;
                        break;
                    case "OCR_Net_Amount":
                        modelinfo.OCR_Net_Amount = item.MyXmlVal;
                        break;
                    case "CR_Tax_Amount":
                        modelinfo.OCR_Tax_Amount = item.MyXmlVal;
                        break;

                    case "OCR_Probable_POs":
                        modelinfo.OCR_Probable_POs = item.MyXmlVal;
                        break;


                    case "OCR_Description":
                        modelinfo.OCR_Description = item.MyXmlVal;
                        break;



                    default:
                        break;
                }
            }

            return modelinfo;
        }


    }
}

