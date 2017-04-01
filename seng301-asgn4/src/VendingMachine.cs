using System;
using System.Collections.Generic;
using System.Linq; // Added with Tony's permission
using Frontend4;
using Frontend4.Hardware;


/**
 * Represents vending machines, fully configured and with all software
 * installed.
 * 
 */
public class VendingMachine {

    private HardwareFacade hardwareFacade;
    private HardwareLogic hl;

    public HardwareFacade Hardware {
        get {
            return this.hardwareFacade;
        }
    }


    /**
     * Creates a standard arrangement for the vending machine. All the
     * components are created and interconnected. The hardware is initially
     * empty. The product kind names and costs are initialized to &quot; &quot;
     * and 1 respectively.
     * 
     * @param coinKinds
     *            The values (in cents) of each kind of coin. The order of the
     *            kinds is maintained. One coin rack is produced for each kind.
     *            Each kind must have a unique, positive value.
     * @param selectionButtonCount
     *            The number of selection buttons on the machine. Must be
     *            positive.
     * @param coinRackCapacity
     *            The maximum capacity of each coin rack in the machine. Must be
     *            positive.
     * @param productRackCapacity
     *            The maximum capacity of each product rack in the machine. Must
     *            be positive.
     * @param receptacleCapacity
     *            The maximum capacity of the coin receptacle, storage bin, and
     *            delivery chute. Must be positive.
     * @throws IllegalArgumentException
     *             If any of the arguments is null, or the size of productCosts
     *             and productNames differ.
     */
    public VendingMachine(Cents[] coinKinds, int selectionButtonCount, int coinRackCapacity, int productRackCapacity, int receptacleCapacity) {
	    this.hardwareFacade = new HardwareFacade(coinKinds, selectionButtonCount, coinRackCapacity, productRackCapacity, receptacleCapacity);

        /* YOU CAN BUILD AND INSTALL THE HARDWARE HERE */
        this.hl = new HardwareLogic(this.hardwareFacade);

    }

    // Configure VendingMachine
    public void Configure (List<ProductKind> products) {
        // send to hardware logic to be loaded via product facade
        this.hl.configurehw(products);
    }

}

/*
 * 
 * 
 */
public class HardwareLogic {

    PaymentFacade payment;
    CommunicationFacade comms;
    ProductFacade prod;

    // This class talks to the facades
    public HardwareLogic(HardwareFacade hardwareFacade) {

        // Create Facades
        this.payment = new PaymentFacade(hardwareFacade);
        this.comms = new CommunicationFacade(hardwareFacade);
        this.prod = new ProductFacade(hardwareFacade);

        // Load Facades into each other
        this.payment.loadFacades(comms, prod);
        this.comms.loadFacades(payment, prod);
        this.prod.loadFacades(payment, comms);

        // Detect if a selection was made
        this.comms.SelectionMade += new EventHandler<SelectionEventArgs>(initiate);

    }

    // Configure the hardware
    public void configurehw (List<ProductKind> products) {
        // send to the product facade
        this.prod.ConfigureHW(products);
    }

    // Launch 
    public void initiate(object sender, SelectionEventArgs e) {

        if (!this.comms.isOutOfOrder()) {
            // if sufficient funds are inserted            
            if (this.payment.isValidTransaction()) {
                // dispense the product               
                this.prod.dispenseProductReady();
                // dispense the change                
                this.payment.dispenseChange();
                // store coins
                this.payment.storeCoins();
            }

        }        

    }

}


/***************************************** PAYMENT FACADE *****************************************/

/* Usage
 * 
 * 
 */
public class PaymentFacade {

    // Events to fire
    public event EventHandler<ErrorEventArgs> error;

    // Local copy of the hardware facade
    private HardwareFacade hw;

    // Variable used in the "API"
    private int fundsAvailable = 0;
    private int change = 0;
    private int productCost = 0;
    private int selectionButtonPressed;    

    // Facade variables
    private CommunicationFacade comm;
    private ProductFacade prod;

    // Local variables used to process logic
    private Dictionary<int, int> coinKindToCoinRackIndex;

    // SETUP

    public PaymentFacade(HardwareFacade hardwarefacade) {
        this.hw = hardwarefacade;
       
        // SUBSCRIBE to events relevant to this Facade

        // Subscribe to Accepted coin events
        this.hw.CoinSlot.CoinAccepted += new EventHandler<CoinEventArgs>(updateCurrentBalance);

        // Setup all the coins racks value to their index to be used in DispenseChange()
        this.coinKindToCoinRackIndex = new Dictionary<int, int>();
        for (int i = 0; i < this.hw.CoinRacks.Length; i++) {
            this.coinKindToCoinRackIndex[this.hw.GetCoinKindForCoinRack(i).Value] = i;
        }

    }

    // Load all the other facades and subscribe to their appropriate events
    public void loadFacades(CommunicationFacade c, ProductFacade p) {
        this.comm = c;
        this.prod = p;

        this.comm.SelectionMade += new EventHandler<SelectionEventArgs>(selectButtonPressed);

    }

    // INTERNAL PROCESSING METHODS    

    // If the coin is updated return the amount of money that has been inserted.
    private void updateCurrentBalance(object sender, CoinEventArgs e) {
        this.fundsAvailable += e.Coin.Value.Value;
    }
    
    // This method retrieves info of the selection made
    private void selectButtonPressed(object sender, SelectionEventArgs e) {
        this.selectionButtonPressed = e.buttonPressIndex;
        this.productCost = e.productCost.Value;
        this.change = this.fundsAvailable - this.productCost;
        
        // Check if the user can buy
        if (this.change < 0) {
            this.error(this, new ErrorEventArgs { message = "You do not have enough funds available to purchase a " + e.product.Name });
        }

    }

    // INTERFACING METHODS

    // INBOUND

    // Insert a coin into the machine
    public void insertCoin(Coin coin) {
        this.hw.CoinSlot.AddCoin(coin);
    }

    // Load the coin racks
    public void loadCoins(int[] coins) {
        this.hw.LoadCoins(coins);
    }

    // Dispense a coin from a coinrack
    public void dispenseCoin(int index) {
        this.hw.CoinRacks[index].ReleaseCoin();
    }

    // Dispense change
    public void dispenseChange() {
        this.fundsAvailable = this.dispenseAction();        
    }

    // This function dispenses the change and returns any overhead credit that 
    // remains for the next transaction as a result of not having enough change to dispense
    private int dispenseAction() {

        // This code is lifted from Tony's Assignment 4 in VendingMachineLogic.cs. Credit where credit is due.
        while (this.change > 0) {
            var coinRacksWithMoney = this.coinKindToCoinRackIndex.Where(ck => ck.Key <= this.change && this.hw.CoinRacks[ck.Value].Count > 0).OrderByDescending(ck => ck.Key);

            if (coinRacksWithMoney.Count() == 0) {
                return this.change;
            }

            var biggestCoinRackCoinKind = coinRacksWithMoney.First().Key;
            var biggestCoinRackIndex = coinRacksWithMoney.First().Value;

            this.change = this.change - biggestCoinRackCoinKind;
            this.dispenseCoin(biggestCoinRackIndex);

        }
        // End of Tony's code

        // Reset funds available so that there is no credit available to next user by accident
        return 0;

    }

    // This function moves the coins inserted by the user from the coin recepticle to 
    // either the coin slots or the coin storage bin.
    public void storeCoins() {
        this.hw.CoinReceptacle.StoreCoins();
    }

    // OUTBOUND

    // Returns how many coins are in a specified coin rack
    public int coinsRemainInRack(int index) {
        return this.hw.CoinRacks[index].Count;
    }

    // Get total funds available
    public int getFundsAvailable() {
        return this.fundsAvailable;
    }

    // Get change needed to be dispensed for this current transaction
    public int changeToBeDispensed() {
        return this.change;
    }

    // Get if the transaction is ok
    public bool isValidTransaction() {
        if (this.fundsAvailable >= this.productCost) {
            return true;
        }
        return false;
    }

}

/***************************************** COMMUNICATION FACADE *****************************************/

/* Usage
 * 
 * 
 */
public class CommunicationFacade {

    // Events to fire
    public event EventHandler<SelectionEventArgs> SelectionMade;
    public event EventHandler<ErrorEventArgs> error;

    // Local copy of the hardware facade
    private HardwareFacade hw;

    // Variable used in the "API"
    private int selectionButtonPressed;
    private string productName;
    private int productCost;
    private int fundsAvailable;
    private bool outOfOrderSignal;
    private bool errorSignal;
    private string errorMsg;

    // Facade variables
    private PaymentFacade paym;
    private ProductFacade prod;

    // Local variables used to process logic
    private Dictionary<SelectionButton, int> selectionButtonToIndex;

    // SETUP

    public CommunicationFacade(HardwareFacade hardwarefacade) {
        this.hw = hardwarefacade;

        // SUBSCRIBE to events relevant to this Facade

        // Subscribe to Accepted coin events
        this.hw.CoinSlot.CoinAccepted += new EventHandler<CoinEventArgs>(updateCurrentBalance);

        // Subscribe to all the selection buttons
        this.selectionButtonToIndex = new Dictionary<SelectionButton, int>();
        for (int i = 0; i < this.hw.SelectionButtons.Length; i++) {
            this.hw.SelectionButtons[i].Pressed += new EventHandler(selectButtonPressed);
            this.selectionButtonToIndex[this.hw.SelectionButtons[i]] = i;
        }

        // Subscribe to machine status events
        this.hw.OutOfOrderLight.Activated += new EventHandler(setMachineNotActive);
        this.hw.OutOfOrderLight.Deactivated += new EventHandler(setMachineActive);

    }

    // Load all the other facades and subscribe to their appropriate events
    public void loadFacades(PaymentFacade pa, ProductFacade p) {
        this.paym = pa;
        this.prod = p;

        // Listen to their error messages from other facades
        this.paym.error += new EventHandler<ErrorEventArgs>(setErrorMessage);
        this.prod.error += new EventHandler<ErrorEventArgs>(setErrorMessage);

    }

    // INTERNAL PROCESSING METHODS

    // If the coin is updated return the amount of money that has been inserted.
    private void updateCurrentBalance(object sender, CoinEventArgs e) {
        this.fundsAvailable += e.Coin.Value.Value;
        // A user action has been detected reset the error signal
        this.errorSignal = false;
    }

    // This method sets which selection button was pressed
    private void selectButtonPressed(object sender, EventArgs e) {
        this.selectionButtonPressed = this.selectionButtonToIndex[(SelectionButton)sender];
        this.productName = this.hw.ProductKinds[this.selectionButtonPressed].Name;
        this.productCost = this.hw.ProductKinds[this.selectionButtonPressed].Cost.Value;
        // Notify other facades that a selection has been made
        this.SelectionMade(this, new SelectionEventArgs() {
            buttonPressIndex = this.selectionButtonPressed,
            productCost = this.hw.ProductKinds[this.selectionButtonPressed].Cost,
            product = this.hw.ProductKinds[this.selectionButtonPressed]
        });
    }

    // This method sets the machine status
    private void setMachineActive(object sender, EventArgs e) {
        this.outOfOrderSignal = false;
    }
    private void setMachineNotActive(object sender, EventArgs e) {
        this.outOfOrderSignal = true;
        this.error(this, new ErrorEventArgs { message = "This machine is out of order" });
    }

    // Set/Display errors
    private void setErrorMessage(object sender, ErrorEventArgs e) {
        this.errorSignal = true;
        this.errorMsg = e.message;
    }

    // INTERFACING METHODS

    // INBOUND

    // Allow user to select a button
    public void selectButton(int index) {
        if (index >= 0 && index < this.hw.SelectionButtons.Length) {
            this.hw.SelectionButtons[index].Press();
        } else {
            this.error(this, new ErrorEventArgs { message = "That is an invalid selection" });
        }
    }


    // OUTBOUND

    // Send to the receiving hardware total amount of VALID funds user has inserted
    public int getFundsInserted() {
        return this.fundsAvailable;
    }

    // Send to the receiving hardware the total amount of VALID funds user has inserted
    public int getSelectionButtonIndex() {
        return this.selectionButtonPressed;
    }

    // Send to the receiving hardware the name of the product
    public string getProductNameSelected() {
        return this.productName;
    }

    // Send to the receiving hardware the cost of the product
    public int getCostOfTheProduct() {
        return this.productCost;
    }

    // Send to the receiving hardware the whether the machine is out of order or not
    public bool isOutOfOrder() {
        return this.outOfOrderSignal;
    }

    // Send to the recieving hardware a signal if an error has been detected and send the message
    public bool errorSignalLine() {
        return this.errorSignal;
    }
    public string errorMessage() {
        return this.errorMsg;
    }

}

/***************************************** PRODUCT FACADE *****************************************/

/* Usage
 * 
 * 
 */
public class ProductFacade {

    // Events to fire
    public event EventHandler<ErrorEventArgs> error;

    // Local copy of the hardware facade
    private HardwareFacade hw;

    // Variable used in the "API"
    private ProductKind product;
    private int selectionButtonPressed;

    // Local variables used to process logic

    // Facade variables
    private PaymentFacade paym;
    private CommunicationFacade comm;    

    // SETUP

    public ProductFacade(HardwareFacade hardwarefacade) {
        this.hw = hardwarefacade;

        // SUBSCRIBE to events relevant to this Facade

    }

    // Load all the other facades and subscribe to their appropriate events
    public void loadFacades(PaymentFacade pa, CommunicationFacade c) {
        this.paym = pa;
        this.comm = c;

        this.comm.SelectionMade += new EventHandler<SelectionEventArgs>(selectButtonPressed);
    }

    // INTERNAL PROCESSING METHODS

    // This method retrieves info of the selection made
    private void selectButtonPressed(object sender, SelectionEventArgs e) {
        this.product = e.product;
        this.selectionButtonPressed = e.buttonPressIndex;
    }

    // INTERFACING METHODS

    // INBOUND

    // Load the product racks
    public void loadProducts(int[] products) {
        this.hw.LoadProducts(products);
    }

    // Dispense a product from a certain product rack (Manual)
    public void dispenseProduct(int index) {
        try {
            this.hw.ProductRacks[index].DispenseProduct();
        } catch (Exception e) {
            this.error(this, new ErrorEventArgs { message = e.Message });
        }
    }

    // This dispenses a product that was at the ready because a selection button was pressed
    public void dispenseProductReady() {
        try {
            this.hw.ProductRacks[this.selectionButtonPressed].DispenseProduct();
        }
        catch (Exception e) {
            this.error(this, new ErrorEventArgs { message = e.Message });
        }
    }

    // Configure the machine with products
    public void ConfigureHW(List<ProductKind> products) {
        try {
            this.hw.Configure(products);
        } catch (Exception e) {
            this.error(this, new ErrorEventArgs { message = e.Message });
        }
    }

    // OUTBOUND

}

/********* EVEN DELEGATES **********/

// Delegates for communication facade
public class SelectionEventArgs : EventArgs {
    public int buttonPressIndex { get; set; }
    public Cents productCost { get; set; }
    public ProductKind product { get; set; }
}

// Delegates for communication facade
public class ErrorEventArgs : EventArgs {
    public string message { get; set; }
}
