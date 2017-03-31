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

        PaymentFacade payment       = new PaymentFacade(this.hardwareFacade);
        CommunicationFacade comms   = new CommunicationFacade(this.hardwareFacade);
        ProductFacade prod          = new ProductFacade(this.hardwareFacade);

    }
}

public class HardwareLogic {

    // This class talks to the facades

}

public class PaymentFacade {

    // Subscribe to events coming from HardwareFacade
    private HardwareFacade hw;

    public PaymentFacade(HardwareFacade hardwarefacade) {
        this.hw = hardwarefacade;
    }

}



/* Usage
 * 
 *  getFundsInserted()          - Int value of the valid money inserted by the user
 *  getSelectionButtonIndex()   - Returns the int value of the selection button that was pressed
 *  getProductNameSelected()    - Returns the name of the product selected by user
 *  getCostOfTheProduct()       - Returns the cost of the product selected by user 
 * 
 */
public class CommunicationFacade {

    // Subscribe to events coming from HardwareFacade
    private HardwareFacade hw;
    private String productSelected;
    private int fundsAvailable;
    private int selectionButtonPressed;
    private Dictionary<SelectionButton, int> selectionButtonToIndex;

    public CommunicationFacade(HardwareFacade hardwarefacade) {
        this.hw = hardwarefacade;
       
        // Subscribe to Accepted coin events
        this.hw.CoinSlot.CoinAccepted += new EventHandler<CoinEventArgs>(updateCurrentBalance);

        // Subscribe to all the selection buttons
        this.selectionButtonToIndex = new Dictionary<SelectionButton, int>();
        for (int i = 0; i < this.hw.SelectionButtons.Length; i++) {
            this.hw.SelectionButtons[i].Pressed += new EventHandler(selectButtonPressed);
            this.selectionButtonToIndex[this.hw.SelectionButtons[i]] = i;
        }
    }

    // INTERNAL PROCESSING METHODS

    // If the coin is updated return the amount of money that has been inserted.
    private void updateCurrentBalance(object sender, CoinEventArgs e) {
        this.fundsAvailable += e.Coin.Value.Value;
    }

    // This method sets which selection button was pressed
    private void selectButtonPressed(object sender, EventArgs e) {
        this.selectionButtonPressed = this.selectionButtonToIndex[(SelectionButton)sender];
    }


    // INTERFACING METHODS

    // This method can be used to display the total amount of VALID funds user has inserted
    public int getFundsInserted() {
        return this.fundsAvailable;
    }

    // This method can be used to display the total amount of VALID funds user has inserted
    public int getSelectionButtonIndex() {
        return this.selectionButtonPressed;
    }

}


public class ProductFacade {

    // Subscribe to events coming from HardwareFacade
    private HardwareFacade hw;

    public ProductFacade(HardwareFacade hardwarefacade) {
        this.hw = hardwarefacade;
    }

}