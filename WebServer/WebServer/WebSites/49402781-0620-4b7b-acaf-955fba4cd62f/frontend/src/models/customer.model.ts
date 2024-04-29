import { Phone } from "./phone.model";

// interface Purchase {
//   phoneId: string;
//   phoneName: string;
//   price: number;
// }

export interface Customer {
  _id: string;
  title: string;
  firstName: string;
  surname: string;
  mobile: string;
  emailAddress: string;
  homeAddress: {
    addressLine1: string;
    town: string;
    countyCity: string;
    eircode: string;
  };
  purchaseHistory: Phone[];
}
