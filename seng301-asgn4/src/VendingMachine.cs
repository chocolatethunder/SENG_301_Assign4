using System;
using System.Collections.Generic;
using Frontend4;
using Frontend4.Hardware;


/**
 * Represents vending machines, fully configured and with all software
 * installed.
 * 
 */
public class VendingMachine {

    private HardwareFacade hardwareFacade;
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
        HardwareLogic hl = new HardwareLogic(this.hardwareFacade);

    }

}

public class HardwareLogic {

    // This class talks to the facades
    public HardwareLogic(HardwareFacade hardwareFacade) {

        // Create Facades
        PaymentFacade payment = new PaymentFacade(hardwareFacade);
        CommunicationFacade comms = new CommunicationFacade(hardwareFacade);
        ProductFacade prod = new ProductFacade(hardwareFacade);

        // Load Facades into each other
        payment.loadFacades(comms, prod);
        comms.loadFacades(payment, prod);
        prod.loadFacades(payment, comms);

        // Build Logic here

    }

}


/***************************************** PAYMENT FACADE *****************************************/

/* Usage
 * 
 * 
 */
public class PaymentFacade {

    // Events to fire

    // Local copy of the hardware facade
    private HardwareFacade hw;

    // Variable used in the "API"
    private int fundsAvailable;
    private int productCost;
    private int selectionButtonPressed;
    private int creditAvailable = 0;
    private int totalFundsAvailable = 0;
    private int change = 0;

    // Facade variables
    private CommunicationFacade comm;
    private ProductFacade prod;

    // SETUP

    public PaymentFacade(HardwareFacade hardwarefacade) {
        this.hw = hardwarefacade;
       
        // SUBSCRIBE to events relevant to this Facade

        // Subscribe to Accepted coin events
        this.hw.CoinSlot.CoinAccepted += new EventHandler<CoinEventArgs>(updateCurrentBalance);

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
        this.change = this.totalFundsAvailable - this.productCost;
    }


    // INTERFACING METHODS

    // Insert a coin into the machine
    public void insertCoin(Coin coin) {
        this.hw.CoinSlot.AddCoin(coin);
    }

    // Load the coin racks
    public void loadCoins(int[] coins) {
        this.hw.LoadCoins(coins);
    }

    // Returns how many coins are in a specified coin rack
    public int coinsRemainInRack(int index) {
        return this.hw.CoinRacks[index].Count;
    }



    // Get available credit from previous transaction
    public void setCredit(int credit) {
        this.creditAvailable = credit;
    }

    // Get total funds available
    public int getTotalFundsAvailable() {
        return (this.fundsAvailable + this.creditAvailable);
    }

    // Get change needed to be dispensed for this current transaction
    public int changeToBeDispensed() {
        return this.change;
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

    // Local copy of the hardware facade
    private HardwareFacade hw;

    // Variable used in the "API"
    private int selectionButtonPressed;
    private string productName;
    private int productCost;
    private int fundsAvailable;

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
    }

    // Load all the other facades and subscribe to their appropriate events
    public void loadFacades(PaymentFacade pa, ProductFacade p) {
        this.paym = pa;
        this.prod = p;
    }

    // INTERNAL PROCESSING METHODS

    // If the coin is updated return the amount of money that has been inserted.
    private void updateCurrentBalance(object sender, CoinEventArgs e) {
        this.fundsAvailable += e.Coin.Value.Value;
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

    // INTERFACING METHODS

}

// Delegates for communication facade
public class SelectionEventArgs : EventArgs {
    public int buttonPressIndex { get; set; }
    public Cents productCost { get; set; }
    public ProductKind product { get; set; }
}


/***************************************** PRODUCT FACADE *****************************************/

/* Usage
 * 
 * 
 */
public class ProductFacade {

    // Events to fire

    // Local copy of the hardware facade
    private HardwareFacade hw;

    // Variable used in the "API"

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
    }

    // INTERNAL PROCESSING METHODS

    // INTERFACING METHODS

}
