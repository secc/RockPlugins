// This code was auto-generated, any manual changes made will be lost.

import { Guid } from "@Obsidian/Types";
import { RegistrationEntryFormBag } from "@Obsidian/ViewModels/Blocks/Event/RegistrationEntry/registrationEntryFormBag";
import { GatewayControlBag } from "@Obsidian/ViewModels/Controls/gatewayControlBag";
import { ListItemBag } from "@Obsidian/ViewModels/Utility/listItemBag";

export type CatalogSettingsInitializationBox = {
    canEdit: boolean;

    catalogUrl?: string | null;

    defaultsText?: string | null;

    pendingCheckouts?: PendingCheckoutBag[] | null;

    templates?: AdminTemplateBag[] | null;
};

export type AdminTemplateBag = {
    catalogDescription?: string | null;

    categoryName?: string | null;

    fees?: AdminFeeBag[] | null;

    id: number;

    imageUrl?: string | null;

    instances?: AdminInstanceBag[] | null;

    isVisible: boolean;

    name?: string | null;

    order: number;

    rockDescription?: string | null;
};

export type AdminInstanceBag = {
    campusName?: string | null;

    eligibleGradeBands: number;

    eventDateSource?: string | null;

    eventDateText?: string | null;

    eventEndDate?: string | null;

    eventStartDate?: string | null;

    hasAccount: boolean;

    id: number;

    isOpen: boolean;

    isVisible: boolean;

    maxAttendees?: number | null;

    name?: string | null;

    order: number;

    price: number;

    registeredCount: number;

    windowText?: string | null;
};

export type AdminFeeBag = {
    id: number;

    items?: AdminFeeItemBag[] | null;

    name?: string | null;

    ruleText?: string | null;
};

export type AdminFeeItemBag = {
    cost: number;

    eligibleGradeBands: number;

    id: number;

    name?: string | null;
};

export type PendingCheckoutBag = {
    canReopen: boolean;

    cartId: number;

    detail?: string | null;

    isTooRecent: boolean;

    lines?: string[] | null;

    payerName?: string | null;

    payerPersonId?: number | null;

    stateText?: string | null;

    stoppedText?: string | null;

    total: number;

    transactionCode?: string | null;

    transactionId?: number | null;
};

export type PendingCheckoutResultBag = {
    errorMessage?: string | null;

    pendingCheckouts?: PendingCheckoutBag[] | null;
};

export type SaveTemplateResultBag = {
    errorMessage?: string | null;

    template?: AdminTemplateBag | null;
};

export type NavigationBag = {
    cartItemCount: number;

    cartUrl?: string | null;

    catalogUrl?: string | null;

    checkoutUrl?: string | null;

    isSignedIn: boolean;

    loginUrl?: string | null;

    pendingCheckoutNotice?: string | null;
};

export type PricedCartBag = {
    cartId?: number | null;

    discounts?: AppliedDiscountBag[] | null;

    discountTotal: number;

    itemCount: number;

    lines?: PricedLineBag[] | null;

    subtotal: number;

    total: number;
};

export type PricedLineBag = {
    addOns?: PricedAddOnBag[] | null;

    basePrice: number;

    dateText?: string | null;

    discountAmount: number;

    discountCode?: string | null;

    editUrl?: string | null;

    isWaitList: boolean;

    lineItemId: number;

    lineTotal: number;

    offeringTitle?: string | null;

    problem?: string | null;

    registrantId: number;

    registrantName?: string | null;

    variantName?: string | null;
};

export type PricedAddOnBag = {
    name?: string | null;

    price: number;
};

export type AppliedDiscountBag = {
    amount: number;

    code?: string | null;
};

export type CatalogInitializationBox = {
    heading?: string | null;

    intro?: string | null;

    navigation?: NavigationBag | null;

    offerings?: OfferingSummaryBag[] | null;

    camps?: CampBag[] | null;

    people?: CampPersonBag[] | null;

    familyCampusId?: number | null;

    preventDuplicateRegistrations: boolean;

    canAddFamilyMembers: boolean;

    gradeOptions?: ListItemBag[] | null;
};

export type CampBag = {
    key?: string | null;

    title?: string | null;

    year?: string | null;

    typeText?: string | null;

    isServing: boolean;

    description?: string | null;

    imageUrl?: string | null;

    minPrice: number;

    campuses?: CampCampusBag[] | null;

    options?: CampOptionBag[] | null;
};

export type CampCampusBag = {
    id: number;

    name?: string | null;
};

export type CampOptionBag = {
    instanceId: number;

    detailUrl?: string | null;

    name?: string | null;

    roleText?: string | null;

    campusId?: number | null;

    campusName?: string | null;

    price: number;

    sessionText?: string | null;

    eligibleGradeBands: number;

    minAge?: number | null;

    maxAge?: number | null;

    spotsRemaining?: number | null;

    waitListEnabled: boolean;
};

export type CampPersonBag = {
    personAliasId: number;

    fullName?: string | null;

    gradeBand: number;

    gradeText?: string | null;

    age?: number | null;

    isSelf: boolean;

    registrations?: CampRegistrationBag[] | null;

    inCartInstanceIds?: number[] | null;
};

export type CampRegistrationBag = {
    campKey?: string | null;

    instanceId: number;

    campusName?: string | null;

    isWaitList: boolean;
};

export type OfferingSummaryBag = {
    campusNames?: string[] | null;

    dateText?: string | null;

    description?: string | null;

    detailUrl?: string | null;

    idKey?: string | null;

    imageUrl?: string | null;

    minPrice: number;

    offeringTypeText?: string | null;

    optionCount: number;

    title?: string | null;
};

export type OfferingDetailInitializationBox = {
    canAddFamilyMembers: boolean;

    editLine?: EditLineBag | null;

    errorMessage?: string | null;

    gradeOptions?: ListItemBag[] | null;

    navigation?: NavigationBag | null;

    offering?: OfferingBag | null;

    people?: RegistrantOptionBag[] | null;
};

export type EditLineBag = {
    discountCode?: string | null;

    feeItemIds?: number[] | null;

    fieldValues?: Record<Guid, unknown> | null;

    isDiscountCodeLocked: boolean;

    isWaitList: boolean;

    lineItemId: number;

    lockedFieldGuids?: Guid[] | null;

    personAliasId: number;

    registrationInstanceId: number;
};

export type StartingValuesBag = {
    fieldValues?: Record<Guid, unknown> | null;

    lockedFieldGuids?: Guid[] | null;
};

export type OfferingBag = {
    addOnGroups?: AddOnGroupBag[] | null;

    dateText?: string | null;

    description?: string | null;

    discountCodeTerm?: string | null;

    feeTerm?: string | null;

    formLists?: FormListsBag | null;

    forms?: RegistrationEntryFormBag[] | null;

    hasDiscountCodes: boolean;

    idKey?: string | null;

    imageUrl?: string | null;

    isSignatureDrawn: boolean;

    offeringTypeText?: string | null;

    registrantTerm?: string | null;

    requiresSignature: boolean;

    signatureDocumentTerm?: string | null;

    title?: string | null;

    variants?: VariantBag[] | null;
};

export type FormListsBag = {
    campuses?: ListItemBag[] | null;

    connectionStatuses?: ListItemBag[] | null;

    ethnicities?: ListItemBag[] | null;

    grades?: ListItemBag[] | null;

    maritalStatuses?: ListItemBag[] | null;

    races?: ListItemBag[] | null;
};

export type VariantBag = {
    basePrice: number;

    campusId?: number | null;

    campusName?: string | null;

    deadlineText?: string | null;

    eligibleGradeBands: number;

    feeItemsRemaining?: Record<number, number> | null;

    id: number;

    name?: string | null;

    sessionText?: string | null;

    spotsRemaining?: number | null;

    waitListEnabled: boolean;
};

export type AddOnGroupBag = {
    id: number;

    isLocked: boolean;

    isRequired: boolean;

    items?: AddOnItemBag[] | null;

    name?: string | null;

    selectionMode: number;
};

export type AddOnItemBag = {
    eligibleGradeBands: number;

    id: number;

    name?: string | null;

    price: number;
};

export type RegistrantOptionBag = {
    fullName?: string | null;

    gradeBand: number;

    gradeText?: string | null;

    isInCart: boolean;

    isSelf: boolean;

    personAliasId: number;
};

export type AddToCartRequestBag = {
    discountCode?: string | null;

    existingSignatureDocumentGuid?: Guid | null;

    feeItemIds?: number[] | null;

    fieldValues?: Record<Guid, unknown> | null;

    lineItemId?: number | null;

    personAliasId: number;

    registrationInstanceId: number;

    signatureData?: string | null;
};

export type AddToCartResultBag = {
    discountNotice?: string | null;

    errorMessage?: string | null;

    isWaitList?: boolean | null;

    navigation?: NavigationBag | null;
};

export type UpdateCartLineResultBag = {
    discountNotice?: string | null;

    errorMessage?: string | null;

    isWaitList?: boolean | null;

    navigation?: NavigationBag | null;

    needsSignature: boolean;
};

export type SignatureRequestBag = {
    documentHtml?: string | null;

    fieldValues?: Record<Guid, unknown> | null;

    personAliasId: number;

    registrationInstanceId: number;

    securityToken?: string | null;

    signature?: unknown;
};

export type AddFamilyMemberRequestBag = {
    birthDate?: string | null;

    firstName?: string | null;

    gradeOffset?: number | null;

    lastName?: string | null;
};

export type CartInitializationBox = {
    cart?: PricedCartBag | null;

    navigation?: NavigationBag | null;
};

export type CartResultBag = {
    cart?: PricedCartBag | null;

    errorMessage?: string | null;
};

export type CheckoutInitializationBox = {
    cart?: PricedCartBag | null;

    gatewayControl?: GatewayControlBag | null;

    navigation?: NavigationBag | null;

    notices?: string[] | null;

    payerEmail?: string | null;

    payerName?: string | null;

    problems?: string[] | null;

    tokenizer?: TokenizerSettingsBag | null;
};

export type TokenizerSettingsBag = {
    gatewayUrl?: string | null;

    publicKey?: string | null;

    scriptUrl?: string | null;
};

export type CheckoutRequestBag = {
    cartId?: number | null;

    expectedTotal: number;

    gatewayToken?: string | null;
};

export type CheckoutResultBag = {
    alreadyProcessed: boolean;

    cart?: PricedCartBag | null;

    cartChanged: boolean;

    errorMessage?: string | null;

    isDeclined: boolean;

    itemDescriptions?: string[] | null;

    problems?: string[] | null;

    success: boolean;

    total: number;

    transactionId?: number | null;
};
